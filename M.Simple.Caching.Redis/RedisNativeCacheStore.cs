using StackExchange.Redis;

namespace M.Simple.Caching.Redis;

internal sealed partial class RedisCacheStore
{
    public async Task<CacheResult<T>> ReadNativeAsync<T>(string key, SimpleNativeCacheOptions options, CancellationToken cancellationToken)
    {
        var db = await Database(cancellationToken).ConfigureAwait(false);
        var result = await db.ScriptEvaluateAsync("""
            local kind = redis.call('TYPE', KEYS[1]).ok
            if kind == 'none' then return {0} end
            if kind ~= 'string' then return {-1} end
            if redis.call('STRLEN', KEYS[1]) > tonumber(ARGV[1]) then return {-2} end
            return {1, redis.call('GET', KEYS[1])}
            """, new RedisKey[] { key }, new RedisValue[] { options.MaxPayloadBytes })
            .WaitAsync(cancellationToken).ConfigureAwait(false);
        var fields = (RedisResult[])result!;
        ValidateNativeStatus((long)fields[0]);
        if ((long)fields[0] == 0) return new(false, default);
        return new(true, NativeCacheSerialization.Deserialize<T>((byte[])fields[1]!, options.SerializerOptions, rawText: true));
    }

    public async Task<bool> NativeExistsAsync(string key, CancellationToken cancellationToken)
    {
        var db = await Database(cancellationToken).ConfigureAwait(false);
        return await db.KeyExistsAsync(key).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<CacheResult<IReadOnlyList<byte[]>>> ReadCollectionAsync(string key, NativeCollectionKind kind, long start, int count,
        SimpleNativeCacheOptions options, CancellationToken cancellationToken)
    {
        var db = await Database(cancellationToken).ConfigureAwait(false);
        var type = kind switch
        {
            NativeCollectionKind.Hash => "hash", NativeCollectionKind.List => "list",
            NativeCollectionKind.Set => "set", NativeCollectionKind.SortedSet => "zset",
            _ => throw new ArgumentException("Unsupported native collection kind.", nameof(kind))
        };
        var result = await db.ScriptEvaluateAsync(NativeCollectionReadScript, new RedisKey[] { key },
            new RedisValue[] { type, start, count, options.MaxPayloadBytes, options.MaxCollectionItems })
            .WaitAsync(cancellationToken).ConfigureAwait(false);
        var fields = (RedisResult[])result!;
        ValidateNativeStatus((long)fields[0]);
        if ((long)fields[0] == 0) return new(false, default);
        return new(true, ((RedisResult[])fields[1]!).Select(value => (byte[])value!).ToArray());
    }

    private static void ValidateNativeStatus(long status)
    {
        if (status == -1) throw new InvalidOperationException("Native Redis value has a different type. Use the corresponding native collection method.");
        if (status == -2) throw new InvalidOperationException("Native payload exceeds MaxPayloadBytes; no partial data was returned.");
        if (status == -3) throw new InvalidOperationException("Native collection exceeds MaxCollectionItems; no partial data was returned.");
    }

    private const string NativeCollectionReadScript = """
        local kind = redis.call('TYPE', KEYS[1]).ok
        if kind == 'none' then return {0} end
        if kind ~= ARGV[1] then return {-1} end
        local first = tonumber(ARGV[2])
        local last = first + tonumber(ARGV[3]) - 1
        local values
        if kind == 'hash' then
            if redis.call('HLEN', KEYS[1]) > tonumber(ARGV[5]) then return {-3} end
            values = redis.call('HGETALL', KEYS[1])
        elseif kind == 'list' then values = redis.call('LRANGE', KEYS[1], first, last)
        elseif kind == 'set' then
            if redis.call('SCARD', KEYS[1]) > tonumber(ARGV[5]) then return {-3} end
            values = redis.call('SMEMBERS', KEYS[1])
        else values = redis.call('ZRANGE', KEYS[1], first, last, 'WITHSCORES') end
        local bytes = 0
        for _, value in ipairs(values) do
            bytes = bytes + string.len(value)
            if bytes > tonumber(ARGV[4]) then return {-2} end
        end
        return {1, values}
        """;
}
