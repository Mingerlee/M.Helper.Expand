using System.Globalization;
using System.Text;

namespace M.Simple.Caching;

internal sealed class SimpleNativeCache : ISimpleNativeCache
{
    private readonly ISimpleNativeCacheStore _store;
    private readonly ISimpleNativeCollectionStore? _collections;
    private readonly SimpleNativeCacheOptions _options;
    private readonly string[] _prefixes;

    public SimpleNativeCache(ISimpleCacheStore store, SimpleCacheOptions business, SimpleNativeCacheOptions options)
    {
        _store = store as ISimpleNativeCacheStore ?? throw new NotSupportedException("This backend does not support native reads.");
        _collections = store as ISimpleNativeCollectionStore;
        _options = options.Snapshot();
        if (_options.Scope == NativeCacheScope.CurrentApplication && string.IsNullOrWhiteSpace(business.KeyPrefix))
            throw new InvalidOperationException("CurrentApplication native reads require a nonempty business KeyPrefix.");
        _prefixes = _options.Scope switch
        {
            NativeCacheScope.CurrentApplication => [business.KeyPrefix],
            NativeCacheScope.Prefixes => _options.Prefixes,
            _ => [""]
        };
    }

    public Task<CacheResult<T>> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key, cancellationToken);
        return _store.ReadNativeAsync<T>(key, _options, cancellationToken);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key, cancellationToken);
        return _store.NativeExistsAsync(key, cancellationToken);
    }

    public async Task<CacheResult<IReadOnlyDictionary<string, byte[]>>> HashGetAllAsync(string key, CancellationToken cancellationToken = default)
    {
        var result = await ReadCollection(key, NativeCollectionKind.Hash, 0, _options.MaxCollectionItems, cancellationToken).ConfigureAwait(false);
        if (!result.Found) return new(false, default);
        var fields = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var utf8 = new UTF8Encoding(false, true);
        for (var i = 0; i < result.Value!.Count; i += 2) fields.Add(utf8.GetString(result.Value[i]), result.Value[i + 1]);
        return new(true, fields);
    }

    public Task<CacheResult<IReadOnlyList<byte[]>>> ListRangeAsync(string key, long start = 0, int count = 100, CancellationToken cancellationToken = default)
        => ReadCollection(key, NativeCollectionKind.List, start, count, cancellationToken);

    public Task<CacheResult<IReadOnlyList<byte[]>>> SetMembersAsync(string key, CancellationToken cancellationToken = default)
        => ReadCollection(key, NativeCollectionKind.Set, 0, _options.MaxCollectionItems, cancellationToken);

    public async Task<CacheResult<IReadOnlyList<NativeSortedSetEntry>>> SortedSetRangeAsync(string key, long start = 0, int count = 100, CancellationToken cancellationToken = default)
    {
        var result = await ReadCollection(key, NativeCollectionKind.SortedSet, start, count, cancellationToken).ConfigureAwait(false);
        if (!result.Found) return new(false, default);
        var entries = new List<NativeSortedSetEntry>();
        for (var i = 0; i < result.Value!.Count; i += 2)
            entries.Add(new(result.Value[i], double.Parse(Encoding.ASCII.GetString(result.Value[i + 1]), CultureInfo.InvariantCulture)));
        return new(true, entries);
    }

    private Task<CacheResult<IReadOnlyList<byte[]>>> ReadCollection(string key, NativeCollectionKind kind, long start, int count, CancellationToken ct)
    {
        ValidateKey(key, ct);
        if (start < 0 || count < 1 || count > _options.MaxCollectionItems || start > long.MaxValue - count)
            throw new ArgumentException("Invalid native collection range.");
        return (_collections ?? throw new NotSupportedException("Native collection methods are supported only by the Redis backend."))
            .ReadCollectionAsync(key, kind, start, count, _options, ct);
    }

    private void ValidateKey(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length > 1024 || !_prefixes.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal)))
            throw new ArgumentException("Full key is too long or outside the native read scope.", nameof(key));
    }
}
