using System.Text;
using System.Text.Json;

namespace M.Simple.Caching;

internal sealed class SimpleCacheAdmin : ISimpleCacheAdmin
{
    private readonly ISimpleCacheAdminStore _store;
    private readonly SimpleCache _cache;
    public string Backend => _store.Backend;
    public string KeyPrefix { get; }

    public SimpleCacheAdmin(ISimpleCacheStore store, SimpleCache cache, SimpleCacheOptions options)
    {
        _store = store as ISimpleCacheAdminStore ?? throw new NotSupportedException("This backend does not support cache management.");
        _cache = cache;
        KeyPrefix = options.KeyPrefix;
        if (string.IsNullOrWhiteSpace(KeyPrefix))
            throw new InvalidOperationException("Cache management requires a nonempty, application-specific KeyPrefix.");
    }

    public CacheStatistics GetStatistics() => _cache.GetStatistics();

    public async Task<CacheScanPage> ScanAsync(string prefix = "", string? cursor = null, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        if (KeyPrefix.Length + prefix.Length > 1024 || pageSize is < 1 or > 200)
            throw new ArgumentException("Invalid prefix or page size (1-200).");
        string? storeCursor = null;
        if (cursor is not null)
        {
            try
            {
                if (cursor.Length > 8192) throw new FormatException();
                var state = JsonSerializer.Deserialize<Cursor>(Convert.FromBase64String(cursor)) ?? throw new FormatException();
                if (state.Scope != KeyPrefix + prefix || state.Backend != Backend) throw new FormatException();
                storeCursor = state.Position;
            }
            catch (Exception error) when (error is FormatException or JsonException)
            { throw new ArgumentException("Invalid cursor for this prefix/backend.", nameof(cursor)); }
        }
        var page = await _store.ScanAsync(KeyPrefix + prefix, storeCursor, pageSize, cancellationToken).ConfigureAwait(false);
        var entries = page.Items.Where(i => i.Key.StartsWith(KeyPrefix + prefix, StringComparison.Ordinal))
            .Select(i => i with { Key = i.Key[KeyPrefix.Length..] }).ToArray();
        var next = page.NextCursor is null ? null : Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(
            new Cursor(KeyPrefix + prefix, Backend, page.NextCursor)));
        return new(entries, next);
    }

    public async Task<CacheEntryDetail?> PeekAsync(string key, CancellationToken cancellationToken = default)
    {
        var full = FullKey(key);
        var detail = await _store.PeekAsync(full, cancellationToken).ConfigureAwait(false);
        return detail is null ? null : detail with { Entry = detail.Entry with { Key = key } };
    }

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _ = FullKey(key);
        return _cache.RemoveManagedAsync(key, _store, cancellationToken);
    }

    private string FullKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length + KeyPrefix.Length > 1024) throw new ArgumentException("Key is too long.", nameof(key));
        return KeyPrefix + key;
    }

    private sealed record Cursor(string Scope, string Backend, string Position);
}
