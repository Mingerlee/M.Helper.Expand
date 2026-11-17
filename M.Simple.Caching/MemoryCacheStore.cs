using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace M.Simple.Caching;

internal sealed class MemoryCacheStore(SimpleMemoryCacheOptions options, TimeProvider clock) : ISimpleCacheStore, IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = options.MaxSizeBytes });
    private readonly object _gate = new();
    private readonly int _maxEntries = options.MaxEntryCount;

    public Task<CacheStoreValue?> ReadAsync(string key, bool refreshSliding, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var entry = Read(key, refreshSliding);
            return Task.FromResult(entry?.Value);
        }
    }

    private Entry? Read(string key, bool refreshSliding)
    {
        if (!_cache.TryGetValue<Entry>(key, out var entry) || entry is null) return null;
        var now = clock.GetUtcNow();
        if (entry.ExpiresAt is { } expires && expires <= now)
        {
            _cache.Remove(key);
            return null;
        }
        if (refreshSliding && entry.Sliding is { } sliding)
        {
            entry.ExpiresAt = Min(now + sliding, entry.Absolute);
            Save(key, entry);
        }
        return entry;
    }

    public Task WriteAsync(string key, CacheStoreValue value, CacheExpiration expiration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_cache.TryGetValue(key, out _) && _cache.Count >= _maxEntries)
                _cache.Compact(Math.Max(0.1, 1.0 / _maxEntries));
            if (!_cache.TryGetValue(key, out _) && _cache.Count >= _maxEntries) return Task.CompletedTask;
            var now = clock.GetUtcNow();
            var absolute = expiration.AbsoluteExpiration is { } abs ? now + abs : (DateTimeOffset?)null;
            var expires = expiration.SlidingExpiration is { } sliding ? Min(now + sliding, absolute) : absolute;
            Save(key, new(value, absolute, expiration.SlidingExpiration, expires));
        }
        return Task.CompletedTask;
    }

    private void Save(string key, Entry entry)
    {
        var settings = new MemoryCacheEntryOptions
        {
            Size = entry.Value.Payload.LongLength + Encoding.UTF8.GetByteCount(key) + Encoding.UTF8.GetByteCount(entry.Value.TypeName) + 128
        };
        if (entry.ExpiresAt is { } expires)
        {
            var remaining = expires - clock.GetUtcNow();
            if (remaining <= TimeSpan.Zero) { _cache.Remove(key); return; }
            settings.AbsoluteExpirationRelativeToNow = remaining;
        }
        _cache.Set(key, entry, settings);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) return Task.FromResult(Read(key, false) is not null);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) _cache.Remove(key);
        return Task.CompletedTask;
    }

    private static DateTimeOffset Min(DateTimeOffset value, DateTimeOffset? cap) => cap is { } absolute && absolute < value ? absolute : value;
    public void Dispose() => _cache.Dispose();

    private sealed class Entry(CacheStoreValue value, DateTimeOffset? absolute, TimeSpan? sliding, DateTimeOffset? expires)
    {
        internal CacheStoreValue Value { get; } = value;
        internal DateTimeOffset? Absolute { get; } = absolute;
        internal TimeSpan? Sliding { get; } = sliding;
        internal DateTimeOffset? ExpiresAt { get; set; } = expires;
    }
}
