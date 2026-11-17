namespace M.Simple.Caching;

/// <summary>Distinguishes a miss from a cached null, zero or false.</summary>
public readonly record struct CacheResult<T>(bool Found, T? Value);

public interface ISimpleCache
{
    Task<CacheResult<T>> TryGetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, SimpleCacheEntryOptions? options = null, CancellationToken cancellationToken = default);
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
        SimpleCacheEntryOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>Storage adapter contract. Implementations must atomically check expiry and touch sliding expiry.</summary>
public interface ISimpleCacheStore
{
    Task<CacheStoreValue?> ReadAsync(string key, bool refreshSliding, CancellationToken cancellationToken);
    Task WriteAsync(string key, CacheStoreValue value, CacheExpiration expiration, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);
    Task RemoveAsync(string key, CancellationToken cancellationToken);
}

/// <summary>Serialized snapshot; adapters must not mutate the payload.</summary>
public sealed record CacheStoreValue(string TypeName, byte[] Payload);
public sealed record CacheExpiration(TimeSpan? AbsoluteExpiration, TimeSpan? SlidingExpiration);
