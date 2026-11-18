namespace M.Simple.Caching;

/// <summary>Management keys are relative to the configured nonempty KeyPrefix. Reads never renew expiry.</summary>
public interface ISimpleCacheAdmin
{
    string Backend { get; }
    string KeyPrefix { get; }
    CacheStatistics GetStatistics();
    Task<CacheScanPage> ScanAsync(string prefix = "", string? cursor = null, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<CacheEntryDetail?> PeekAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public sealed record CacheEntryInfo(string Key, string TypeName, long SizeBytes,
    DateTimeOffset? ExpiresAt, DateTimeOffset? AbsoluteExpiresAt, TimeSpan? SlidingExpiration);
public sealed record CacheEntryDetail(CacheEntryInfo Entry, string Json);
public sealed record CacheScanPage(IReadOnlyList<CacheEntryInfo> Items, string? NextCursor);
public sealed record CacheStatistics(DateTimeOffset StartedAt, long Hits, long Misses, long Loads,
    long LoadFailures, long Writes, long Removes);

/// <summary>Optional adapter management capability. Keys include the complete storage prefix.</summary>
public interface ISimpleCacheAdminStore
{
    string Backend { get; }
    Task<CacheScanPage> ScanAsync(string prefix, string? cursor, int pageSize, CancellationToken cancellationToken);
    Task<CacheEntryDetail?> PeekAsync(string key, CancellationToken cancellationToken);
    Task<bool> RemoveManagedAsync(string key, CancellationToken cancellationToken);
}
