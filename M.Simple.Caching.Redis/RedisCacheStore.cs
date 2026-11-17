using StackExchange.Redis;

namespace M.Simple.Caching.Redis;

internal sealed class RedisCacheStore : ISimpleCacheStore, IDisposable
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

    private static long Milliseconds(TimeSpan? duration) => duration is { } value ? (long)Math.Ceiling(value.TotalMilliseconds) : 0;

    private const string WriteScript = """
        local t = redis.call('TIME')
        local now = tonumber(t[1]) * 1000 + math.floor(tonumber(t[2]) / 1000)
        local absolute = tonumber(ARGV[3])
        local sliding = tonumber(ARGV[4])
        if absolute > 0 then absolute = now + absolute end
        local expires = absolute
        if sliding > 0 and (expires == 0 or now + sliding < expires) then expires = now + sliding end
        redis.call('HSET', KEYS[1], 'type', ARGV[1], 'payload', ARGV[2], 'absolute', absolute, 'sliding', sliding, 'expires', expires)
        if expires > 0 then redis.call('PEXPIREAT', KEYS[1], expires) else redis.call('PERSIST', KEYS[1]) end
        return 1
        """;

    private const string ReadScript = """
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
