using StackExchange.Redis;

namespace M.Simple.Caching.Redis;

internal sealed class RedisCacheStore : ISimpleCacheStore, ISimpleCacheAdminStore, IDisposable
{
    private readonly Lazy<Task<ConnectionMultiplexer>> _connection;
    private readonly int _database;
    private int _disposed;

    internal RedisCacheStore(SimpleRedisCacheOptions options)
    {
        var configuration = ConfigurationOptions.Parse(options.ConnectionString);
        configuration.AbortOnConnectFail = false;
        _connection = new(() => ConnectionMultiplexer.ConnectAsync(configuration));
        _database = options.Database;
    }

    private async Task<IDatabase> Database(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var connection = await _connection.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
        return connection.GetDatabase(_database);
    }

    public async Task<CacheStoreValue?> ReadAsync(string key, bool refreshSliding, CancellationToken cancellationToken)
    {
        var db = await Database(cancellationToken).ConfigureAwait(false);
        var result = await db.ScriptEvaluateAsync(ReadScript, new RedisKey[] { key },
            new RedisValue[] { refreshSliding ? 1 : 0, 0 }).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (result.IsNull) return null;
        var values = (RedisResult[])result!;
        return new((string)values[0]!, (byte[])values[1]!);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        var db = await Database(cancellationToken).ConfigureAwait(false);
        var result = await db.ScriptEvaluateAsync(ReadScript, new RedisKey[] { key },
            new RedisValue[] { 0, 1 }).WaitAsync(cancellationToken).ConfigureAwait(false);
        return (long)result == 1;
    }

    public async Task WriteAsync(string key, CacheStoreValue value, CacheExpiration expiration, CancellationToken cancellationToken)
    {
        var db = await Database(cancellationToken).ConfigureAwait(false);
        await db.ScriptEvaluateAsync(WriteScript, new RedisKey[] { key },
            new RedisValue[] { value.TypeName, value.Payload, Milliseconds(expiration.AbsoluteExpiration), Milliseconds(expiration.SlidingExpiration) })
            .WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        var db = await Database(cancellationToken).ConfigureAwait(false);
        await db.KeyDeleteAsync(key).WaitAsync(cancellationToken).ConfigureAwait(false);
    }


    public string Backend => "Redis";

    public async Task<CacheScanPage> ScanAsync(string prefix, string? cursor, int pageSize, CancellationToken cancellationToken)
    {
        var db = await Database(cancellationToken).ConfigureAwait(false);
        var connection = await _connection.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
        var servers = connection.GetEndPoints().Select(endpoint => connection.GetServer(endpoint))
            .Where(s => s.IsConnected && !s.IsReplica && s.ServerType != ServerType.Sentinel)
            .OrderBy(s => s.EndPoint.ToString(), StringComparer.Ordinal).ToArray();
        if (servers.Length == 0) throw new InvalidOperationException("No connected Redis primary is available for SCAN.");
        ScanCursor state;
        try { state = cursor is null ? new(servers[0].EndPoint.ToString()!, "0", 0) :
            System.Text.Json.JsonSerializer.Deserialize<ScanCursor>(cursor) ?? throw new ArgumentException("Invalid Redis cursor."); }
        catch (System.Text.Json.JsonException) { throw new ArgumentException("Invalid Redis cursor."); }
        var index = Array.FindIndex(servers, s => s.EndPoint.ToString() == state.Server);
        if (index < 0 || !ulong.TryParse(state.Cursor, out _) || state.Offset < 0)
            throw new ArgumentException("Redis topology changed or cursor is invalid. Restart the scan.");
        var result = (RedisResult[])(await servers[index].ExecuteAsync(db.Database, "SCAN",
            new object[] { state.Cursor, "MATCH", EscapePattern(prefix) + "*", "COUNT", 100 }, CommandFlags.None)
            .WaitAsync(cancellationToken).ConfigureAwait(false))!;
        var next = (string)result[0]!;
        var keys = (RedisResult[])result[1]!;
        if (state.Offset > keys.Length) throw new ArgumentException("Redis cursor is no longer valid. Restart the scan.");
        var items = new List<CacheEntryInfo>();
        var position = state.Offset;
        for (; position < keys.Length && position - state.Offset < pageSize; position++)
        {
            var key = (string)keys[position]!;
            if (!key.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var detail = await InspectAsync(key, false, cancellationToken).ConfigureAwait(false);
            if (detail is not null) items.Add(detail.Entry);
        }
        ScanCursor? continuation = position < keys.Length ? state with { Offset = position } :
            next != "0" ? new(state.Server, next, 0) :
            index + 1 < servers.Length ? new(servers[index + 1].EndPoint.ToString()!, "0", 0) : null;
        return new(items, continuation is null ? null : System.Text.Json.JsonSerializer.Serialize(continuation));
    }

    public Task<CacheEntryDetail?> PeekAsync(string key, CancellationToken cancellationToken) => InspectAsync(key, true, cancellationToken);

    private async Task<CacheEntryDetail?> InspectAsync(string key, bool includePayload, CancellationToken cancellationToken)
    {
        var db = await Database(cancellationToken).ConfigureAwait(false);
        var result = await db.ScriptEvaluateAsync(PeekScript, new RedisKey[] { key }, new RedisValue[] { includePayload ? 1 : 0 }).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (result.IsNull) return null;
        var values = (RedisResult[])result!;
        var payload = (byte[])values[1]!;
        static DateTimeOffset? Date(RedisResult value) => (long)value is var time && time > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(time) : null;
        var sliding = (long)values[3];
        return new(new(key, (string)values[0]!, (long)values[5], Date(values[4]), Date(values[2]),
            sliding > 0 ? TimeSpan.FromMilliseconds(sliding) : null), System.Text.Encoding.UTF8.GetString(payload));
    }

    public async Task<bool> RemoveManagedAsync(string key, CancellationToken cancellationToken)
    {
        var db = await Database(cancellationToken).ConfigureAwait(false);
        var result = await db.ScriptEvaluateAsync("""
            local t = redis.call('TYPE', KEYS[1])
            if t.ok ~= 'hash' or redis.call('HEXISTS', KEYS[1], 'type') == 0 or redis.call('HEXISTS', KEYS[1], 'payload') == 0 then return 0 end
            local removed = redis.pcall('UNLINK', KEYS[1])
            if type(removed) == 'table' and removed.err then
                local message = string.lower(removed.err)
                if string.find(message, 'unknown command', 1, true) or string.find(message, 'unknown redis command', 1, true) then
                    return redis.call('DEL', KEYS[1])
                end
                return redis.error_reply(removed.err)
            end
            return removed
            """, new RedisKey[] { key }).WaitAsync(cancellationToken).ConfigureAwait(false);
        return (long)result > 0;
    }

    private static string EscapePattern(string prefix) => System.Text.RegularExpressions.Regex.Replace(prefix, @"([\\*?\[\]])", @"\$1");
    private sealed record ScanCursor(string Server, string Cursor, int Offset);
    private const string PeekScript = """
        local t = redis.call('TYPE', KEYS[1])
        if t.ok ~= 'hash' then return false end
        local fields = redis.call('HMGET', KEYS[1], 'type', 'payload', 'absolute', 'sliding', 'expires')
        if not fields[1] or not fields[2] or not fields[3] or not fields[4] or not fields[5] then return false end
        local t = redis.call('TIME')
        local now = tonumber(t[1]) * 1000 + math.floor(tonumber(t[2]) / 1000)
        local expires = tonumber(fields[5]) or 0
        if expires > 0 and expires <= now then return false end
        local size = string.len(fields[2])
        if ARGV[1] ~= '1' then fields[2] = '' end
        fields[6] = size
        return fields
        """;

    private static long Milliseconds(TimeSpan? duration) => duration is { } value ? (long)Math.Ceiling(value.TotalMilliseconds) : 0;

    private const string WriteScript = """
        -- Redis 3.2 needs effects replication to write after TIME.
        redis.replicate_commands()
        local t = redis.call('TIME')
        local now = tonumber(t[1]) * 1000 + math.floor(tonumber(t[2]) / 1000)
        local absolute = tonumber(ARGV[3])
        local sliding = tonumber(ARGV[4])
        if absolute > 0 then absolute = now + absolute end
        local expires = absolute
        if sliding > 0 and (expires == 0 or now + sliding < expires) then expires = now + sliding end
        redis.call('HMSET', KEYS[1], 'type', ARGV[1], 'payload', ARGV[2], 'absolute', absolute, 'sliding', sliding, 'expires', expires)
        if expires > 0 then redis.call('PEXPIREAT', KEYS[1], expires) else redis.call('PERSIST', KEYS[1]) end
        return 1
        """;

    private const string ReadScript = """
        -- Sliding reads and expired-entry cleanup may write after TIME.
        redis.replicate_commands()
        local fields = redis.call('HMGET', KEYS[1], 'type', 'payload', 'absolute', 'sliding', 'expires')
        if not fields[1] then
            if ARGV[2] == '1' then return 0 else return false end
        end
        local t = redis.call('TIME')
        local now = tonumber(t[1]) * 1000 + math.floor(tonumber(t[2]) / 1000)
        local expires = tonumber(fields[5]) or 0
        if expires > 0 and expires <= now then
            redis.call('DEL', KEYS[1])
            if ARGV[2] == '1' then return 0 else return false end
        end
        local sliding = tonumber(fields[4]) or 0
        local absolute = tonumber(fields[3]) or 0
        if ARGV[1] == '1' and sliding > 0 then
            expires = now + sliding
            if absolute > 0 and absolute < expires then expires = absolute end
            redis.call('HSET', KEYS[1], 'expires', expires)
            redis.call('PEXPIREAT', KEYS[1], expires)
        end
        if ARGV[2] == '1' then return 1 end
        return { fields[1], fields[2] }
        """;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0 || !_connection.IsValueCreated) return;
        _ = _connection.Value.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully) task.Result.Dispose();
            else _ = task.Exception;
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
