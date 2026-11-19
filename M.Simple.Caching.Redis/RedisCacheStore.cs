using StackExchange.Redis;

namespace M.Simple.Caching.Redis;

internal sealed class RedisCacheStore : ISimpleCacheStore, ISimpleCacheAdminStore, IDisposable
{
    private readonly Lazy<Task<ConnectionMultiplexer>> _connection;
    private readonly int _database;
    private readonly bool _native;
    private int _disposed;

    internal RedisCacheStore(SimpleRedisCacheOptions options, CacheDashboardScope scope = CacheDashboardScope.CurrentApplication)
    {
        var configuration = ConfigurationOptions.Parse(options.ConnectionString);
        configuration.AbortOnConnectFail = false;
        _connection = new(() => ConnectionMultiplexer.ConnectAsync(configuration));
        _database = options.Database;
        _native = scope is CacheDashboardScope.All or CacheDashboardScope.Prefixes;
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
        if (result.IsNull) return _native ? await InspectNativeAsync(db, key, includePayload, cancellationToken).ConfigureAwait(false) : null;
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
            if ARGV[1] ~= '1' then
            if t.ok ~= 'hash' then return 0 end
            local fields = redis.call('HMGET', KEYS[1], 'type', 'payload', 'absolute', 'sliding', 'expires')
            for i = 1, 5 do if not fields[i] then return 0 end end
            end
            local removed = redis.pcall('UNLINK', KEYS[1])
            if type(removed) == 'table' and removed.err then
                local message = string.lower(removed.err)
                if string.find(message, 'unknown command', 1, true) or string.find(message, 'unknown redis command', 1, true) then
                    return redis.call('DEL', KEYS[1])
                end
                return redis.error_reply(removed.err)
            end
            return removed
            """, new RedisKey[] { key }, new RedisValue[] { _native ? 1 : 0 }).WaitAsync(cancellationToken).ConfigureAwait(false);
        return (long)result > 0;
    }

    private static string EscapePattern(string prefix) => System.Text.RegularExpressions.Regex.Replace(prefix, @"([\\*?\[\]])", @"\$1");
    private async Task<CacheEntryDetail?> InspectNativeAsync(IDatabase db, string key, bool includePayload, CancellationToken cancellationToken)
    {
        var result = await db.ScriptEvaluateAsync(NativePeekScript, new RedisKey[] { key },
            new RedisValue[] { includePayload ? 1 : 0 }).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (result.IsNull) return null;
        var fields = (RedisResult[])result!;
        var type = (string)fields[0]!;
        var ttl = (long)fields[1];
        var length = (long)fields[2];
        var raw = (RedisResult[])fields[3]!;
        // Base64 preserves arbitrary binary strings, hash fields and collection members losslessly.
        var utf8 = new System.Text.UTF8Encoding(false, true);
        var values = raw.Select(value =>
        {
            var bytes = (byte[])value!;
            string? text;
            try { text = utf8.GetString(bytes); }
            catch (System.Text.DecoderFallbackException) { text = null; }
            return new { text, base64 = Convert.ToBase64String(bytes) };
        }).ToArray();
        var json = includePayload ? System.Text.Json.JsonSerializer.Serialize(new
        {
            redisType = type, length, values,
            truncated = (long)fields[4] == 1,
            note = "UTF-8 text plus lossless base64; hash: field/value pairs; zset: member/score pairs; stream: record id followed by field/value pairs. Preview: at most 100 values, 4096 bytes per value; string: first 65536 bytes."
        }) : "";
        return new(new(key, "Redis:" + type, type == "string" ? length : -1,
            ttl >= 0 ? DateTimeOffset.UtcNow.AddMilliseconds(ttl) : null, null, null)
        { StorageType = type, ItemCount = type == "string" ? null : length >= 0 ? length : null }, json);
    }

    private const string NativePeekScript = """
        local kind = redis.call('TYPE', KEYS[1]).ok
        if kind == 'none' then return false end
        if kind == 'hash' then
            local f = redis.call('HMGET', KEYS[1], 'type', 'payload', 'absolute', 'sliding', 'expires')
            if f[1] and f[2] and f[3] and f[4] and f[5] then
                local t = redis.call('TIME')
                local now = tonumber(t[1]) * 1000 + math.floor(tonumber(t[2]) / 1000)
                if tonumber(f[5]) and tonumber(f[5]) > 0 and tonumber(f[5]) <= now then return false end
            end
        end
        local ttl = redis.call('PTTL', KEYS[1])
        local count = -1
        if kind == 'string' then count = redis.call('STRLEN', KEYS[1])
        elseif kind == 'hash' then count = redis.call('HLEN', KEYS[1])
        elseif kind == 'list' then count = redis.call('LLEN', KEYS[1])
        elseif kind == 'set' then count = redis.call('SCARD', KEYS[1])
        elseif kind == 'zset' then count = redis.call('ZCARD', KEYS[1])
        elseif kind == 'stream' then count = redis.call('XLEN', KEYS[1]) end
        local values = {}
        local truncated = 0
        if ARGV[1] == '1' then
            if kind == 'string' then
                values = {redis.call('GETRANGE', KEYS[1], 0, 65535)}
                if count > 65536 then truncated = 1 end
            else
                local sampled = {}
                if kind == 'hash' then
                    local page = redis.call('HSCAN', KEYS[1], 0, 'COUNT', 50)
                    sampled = page[2]
                    if page[1] ~= '0' then truncated = 1 end
                elseif kind == 'list' then sampled = redis.call('LRANGE', KEYS[1], 0, 99)
                elseif kind == 'set' then
                    local page = redis.call('SSCAN', KEYS[1], 0, 'COUNT', 100)
                    sampled = page[2]
                    if page[1] ~= '0' then truncated = 1 end
                elseif kind == 'zset' then sampled = redis.call('ZRANGE', KEYS[1], 0, 49, 'WITHSCORES')
                elseif kind == 'stream' then
                    local records = redis.call('XRANGE', KEYS[1], '-', '+', 'COUNT', 10)
                    for _, record in ipairs(records) do
                        sampled[#sampled + 1] = record[1]
                        for _, field in ipairs(record[2]) do
                            if #sampled < 100 then sampled[#sampled + 1] = field else truncated = 1 end
                        end
                    end
                    if count > #records then truncated = 1 end
                else truncated = 1 end
                for i = 1, math.min(#sampled, 100) do
                    values[i] = string.sub(sampled[i], 1, 4096)
                    if string.len(sampled[i]) > 4096 then truncated = 1 end
                end
                if #sampled > 100 or count > ((kind == 'hash' or kind == 'zset') and 50 or 100) then truncated = 1 end
            end
        end
        return {kind, ttl, count, values, truncated}
        """;
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
