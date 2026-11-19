using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace M.Simple.Caching;

internal sealed class MemoryCacheStore(SimpleMemoryCacheOptions options, TimeProvider clock) : ISimpleCacheStore, ISimpleCacheAdminStore, ISimpleNativeCacheStore, IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = options.MaxSizeBytes });
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private readonly int _maxEntries = options.MaxEntryCount;
    public string Backend => "Memory";

    public async Task<CacheResult<T>> ReadNativeAsync<T>(string key, SimpleNativeCacheOptions options, CancellationToken cancellationToken)
    {
        var entry = await ReadAsync(key, false, cancellationToken).ConfigureAwait(false);
        if (entry is null) return new(false, default);
        if (entry.Payload.Length > options.MaxPayloadBytes) throw new InvalidOperationException("Native payload exceeds MaxPayloadBytes.");
        return new(true, NativeCacheSerialization.Deserialize<T>(entry.Payload, options.SerializerOptions));
    }

    public Task<bool> NativeExistsAsync(string key, CancellationToken cancellationToken) => ExistsAsync(key, cancellationToken);

    public Task<CacheStoreValue?> ReadAsync(string key, bool refreshSliding, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) return Task.FromResult(Read(key, refreshSliding)?.Value);
    }

    private Entry? Read(string key, bool refreshSliding)
    {
        if (!_cache.TryGetValue<Entry>(key, out var entry) || entry is null)
        {
            _entries.TryRemove(key, out _);
            return null;
        }
        var now = clock.GetUtcNow();
        if (entry.ExpiresAt is { } expires && expires <= now)
        {
            _cache.Remove(key);
            _entries.TryRemove(key, out _);
            return null;
        }
        if (refreshSliding && entry.Sliding is { } sliding)
        {
            entry = new(entry.Value, entry.Absolute, entry.Sliding, Min(now + sliding, entry.Absolute));
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
            if (remaining <= TimeSpan.Zero) { _cache.Remove(key); _entries.TryRemove(key, out _); return; }
            settings.AbsoluteExpirationRelativeToNow = remaining;
        }
        // Remove only this generation; a delayed eviction must not remove a newer entry's index.
        settings.RegisterPostEvictionCallback((_, value, _, _) =>
            ((ICollection<KeyValuePair<string, Entry>>)_entries).Remove(new(key, (Entry)value!)));
        _entries[key] = entry;
        _cache.Set(key, entry, settings);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) return Task.FromResult(Read(key, false) is not null);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken) => RemoveManagedAsync(key, cancellationToken);

    public Task<bool> RemoveManagedAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var found = Read(key, false) is not null;
            _cache.Remove(key);
            _entries.TryRemove(key, out _);
            return Task.FromResult(found);
        }
    }

    public Task<CacheScanPage> ScanAsync(string prefix, string? cursor, int pageSize, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var keys = _entries.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal) &&
                (cursor is null || StringComparer.Ordinal.Compare(k, cursor) > 0)).Order(StringComparer.Ordinal).ToArray();
            var items = new List<CacheEntryInfo>();
            string? last = null;
            var visited = 0;
            foreach (var key in keys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                last = key;
                visited++;
                var entry = Read(key, false);
                if (entry is not null) items.Add(Info(key, entry));
                if (items.Count == pageSize) break;
            }
            return Task.FromResult(new CacheScanPage(items, visited < keys.Length ? last : null));
        }
    }

    public Task<CacheEntryDetail?> PeekAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var entry = Read(key, false);
            return Task.FromResult(entry is null ? null : new CacheEntryDetail(Info(key, entry), Encoding.UTF8.GetString(entry.Value.Payload)));
        }
    }

    private static CacheEntryInfo Info(string key, Entry entry) =>
        new(key, entry.Value.TypeName, entry.Value.Payload.LongLength, entry.ExpiresAt, entry.Absolute, entry.Sliding);
    private static DateTimeOffset Min(DateTimeOffset value, DateTimeOffset? cap) => cap is { } absolute && absolute < value ? absolute : value;
    public void Dispose() => _cache.Dispose();
    private sealed class Entry(CacheStoreValue value, DateTimeOffset? absolute, TimeSpan? sliding, DateTimeOffset? expiresAt)
    {
        internal CacheStoreValue Value { get; } = value;
        internal DateTimeOffset? Absolute { get; } = absolute;
        internal TimeSpan? Sliding { get; } = sliding;
        internal DateTimeOffset? ExpiresAt { get; } = expiresAt;
    }
}
