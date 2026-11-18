using System.Security.Cryptography;
using System.Text.Json;

namespace M.Simple.Caching;

internal sealed class SimpleCacheAdmin : ISimpleCacheAdmin
{
    private readonly ISimpleCacheAdminStore _store;
    private readonly SimpleCache _cache;
    private readonly string[] _prefixes;
    public string Backend => _store.Backend;
    public string KeyPrefix { get; }
    public CacheDashboardScope Scope { get; }
    public IReadOnlyList<string> Prefixes => Array.AsReadOnly(_prefixes);

    public SimpleCacheAdmin(ISimpleCacheStore store, SimpleCache cache, SimpleCacheOptions options, SimpleCacheManagementOptions management)
    {
        _store = store as ISimpleCacheAdminStore ?? throw new NotSupportedException("This backend does not support cache management.");
        _cache = cache;
        KeyPrefix = options.KeyPrefix;
        var snapshot = management.Snapshot();
        Scope = snapshot.Scope;
        if (Scope == CacheDashboardScope.CurrentApplication && string.IsNullOrWhiteSpace(KeyPrefix))
            throw new InvalidOperationException("CurrentApplication management requires a nonempty, application-specific KeyPrefix.");
        _prefixes = Scope switch
        {
            CacheDashboardScope.CurrentApplication => [KeyPrefix],
            CacheDashboardScope.Prefixes => snapshot.Prefixes,
            _ => [""]
        };
    }

    public CacheStatistics GetStatistics() => _cache.GetStatistics();

    public async Task<CacheScanPage> ScanAsync(string prefix = "", string? cursor = null, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        var query = Scope == CacheDashboardScope.CurrentApplication ? KeyPrefix + prefix : prefix;
        if (query.Length > 1024 || pageSize is < 1 or > 200)
            throw new ArgumentException("Invalid prefix or page size (1-200).");

        // Intersect the search with disjoint allowed partitions; never scan outside them.
        var partitions = _prefixes.Select(p => query.StartsWith(p, StringComparison.Ordinal) ? query :
                p.StartsWith(query, StringComparison.Ordinal) ? p : null)
            .Where(p => p is not null).Cast<string>().ToArray();
        var scope = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { Scope, Prefixes = _prefixes, Query = query })));
        var index = 0;
        string? position = null;
        if (cursor is not null)
        {
            try
            {
                if (cursor.Length > 8192) throw new FormatException();
                var state = JsonSerializer.Deserialize<Cursor>(Convert.FromBase64String(cursor)) ?? throw new FormatException();
                if (state.Scope != scope || state.Backend != Backend || state.Index < 0 || state.Index >= partitions.Length)
                    throw new FormatException();
                index = state.Index;
                position = state.Position;
            }
            catch (Exception error) when (error is FormatException or JsonException)
            { throw new ArgumentException("Invalid cursor for this scope/backend/query.", nameof(cursor)); }
        }
        if (partitions.Length == 0) return new([], null);

        var entries = new List<CacheEntryInfo>();
        Cursor? next = null;
        while (index < partitions.Length && entries.Count < pageSize)
        {
            var page = await _store.ScanAsync(partitions[index], position, pageSize - entries.Count, cancellationToken).ConfigureAwait(false);
            entries.AddRange(page.Items.Where(i => i.Key.StartsWith(partitions[index], StringComparison.Ordinal))
                .Select(i => Scope == CacheDashboardScope.CurrentApplication ? i with { Key = i.Key[KeyPrefix.Length..] } : i));
            if (page.NextCursor is not null)
            {
                next = new(scope, Backend, index, page.NextCursor);
                break; // Redis may return an empty SCAN batch; let the caller continue, keeping work bounded.
            }
            position = null;
            index++;
            next = index < partitions.Length ? new(scope, Backend, index, null) : null;
        }
        return new(entries, next is null ? null : Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(next)));
    }

    public async Task<CacheEntryDetail?> PeekAsync(string key, CancellationToken cancellationToken = default)
    {
        var detail = await _store.PeekAsync(FullKey(key), cancellationToken).ConfigureAwait(false);
        return detail is null ? null : detail with { Entry = detail.Entry with { Key = key } };
    }

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        => _cache.RemoveManagedAsync(FullKey(key), _store, cancellationToken);

    private string FullKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var full = Scope == CacheDashboardScope.CurrentApplication ? KeyPrefix + key : key;
        if (full.Length > 1024 || !_prefixes.Any(p => full.StartsWith(p, StringComparison.Ordinal)))
            throw new ArgumentException("Key is too long or outside the management scope.", nameof(key));
        return full;
    }

    private sealed record Cursor(string Scope, string Backend, int Index, string? Position);
}
