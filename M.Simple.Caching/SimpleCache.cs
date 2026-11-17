using System.Collections.Concurrent;
using System.Text.Json;

namespace M.Simple.Caching;

public sealed class SimpleCache : ISimpleCache, IDisposable
{
    private readonly ISimpleCacheStore _store;
    private readonly SimpleCacheOptions _options;
    private readonly ConcurrentDictionary<string, PendingFill> _fills = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim[] _writeGates = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
    private readonly CancellationTokenSource _shutdown = new();
    private int _disposed;

    public SimpleCache(ISimpleCacheStore store, SimpleCacheOptions options)
    {
        options.Validate();
        _store = store;
        _options = options.Snapshot();
    }

    public Task<CacheResult<T>> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
        => ReadAsync<T>(FullKey(key), cancellationToken);

    private async Task<CacheResult<T>> ReadAsync<T>(string key, CancellationToken cancellationToken)
    {
        var value = await _store.ReadAsync(key, true, cancellationToken).ConfigureAwait(false);
        if (value is null) return new(false, default);
        if (value.TypeName != TypeName<T>())
            throw new InvalidOperationException("The cache key contains a different value type. Use distinct keys for distinct types.");
        return new(true, JsonSerializer.Deserialize<T>(value.Payload, _options.SerializerOptions));
    }

    public async Task SetAsync<T>(string key, T value, SimpleCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var fullKey = FullKey(key);
        var policy = (options ?? new()).Snapshot();
        var entry = Serialize(value);
        var gate = Gate(fullKey);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            InvalidateFill(fullKey);
            await _store.WriteAsync(fullKey, entry, Expiration(policy, value is null), cancellationToken).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
        SimpleCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var fullKey = FullKey(key);
        var policy = (options ?? new()).Snapshot();
        var found = await ReadAsync<T>(fullKey, cancellationToken).ConfigureAwait(false);
        if (found.Found) return found.Value!;
        cancellationToken.ThrowIfCancellationRequested();

        PendingFill? candidate = null;
        candidate = new(typeof(T), () => FillAsync(fullKey, factory, policy, candidate!));
        var pending = _fills.GetOrAdd(fullKey, candidate);
        if (pending.ValueType != typeof(T))
            throw new InvalidOperationException("The cache key is being loaded as a different value type.");
        // Caller cancellation only stops this waiter; it does not cancel the shared load.
        return (T)(await pending.Task.Value.WaitAsync(cancellationToken).ConfigureAwait(false))!;
    }

    private async Task<object?> FillAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
        SimpleCacheEntryOptions policy, PendingFill pending)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
        timeout.CancelAfter(_options.FactoryTimeout);
        try
        {
            var cached = await ReadAsync<T>(key, timeout.Token).ConfigureAwait(false);
            if (cached.Found) return cached.Value;
            var value = await factory(timeout.Token).WaitAsync(timeout.Token).ConfigureAwait(false);
            if (value is null && !(policy.CacheNullValues ?? _options.CacheNullValues)) return value;
            var entry = Serialize(value);
            var gate = Gate(key);
            await gate.WaitAsync(timeout.Token).ConfigureAwait(false);
            try
            {
                // Explicit Set/Remove in this process wins over a factory that started earlier.
                if (!pending.Invalidated)
                    await _store.WriteAsync(key, entry, Expiration(policy, value is null), timeout.Token).ConfigureAwait(false);
            }
            finally { gate.Release(); }
            return value;
        }
        finally { ((ICollection<KeyValuePair<string, PendingFill>>)_fills).Remove(new(key, pending)); }
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => _store.ExistsAsync(FullKey(key), cancellationToken);

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = FullKey(key);
        var gate = Gate(fullKey);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            InvalidateFill(fullKey);
            await _store.RemoveAsync(fullKey, cancellationToken).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    private void InvalidateFill(string key)
    {
        if (_fills.TryRemove(key, out var pending)) pending.Invalidated = true;
    }

    private CacheStoreValue Serialize<T>(T value)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(value, _options.SerializerOptions);
        if (payload.Length > _options.MaxPayloadBytes)
            throw new ArgumentException("The serialized cache value exceeds MaxPayloadBytes.", nameof(value));
        return new(TypeName<T>(), payload);
    }

    private CacheExpiration Expiration(SimpleCacheEntryOptions options, bool isNull)
    {
        var absolute = options.NeverExpires ? null : options.AbsoluteExpirationRelativeToNow;
        if (!options.NeverExpires && absolute is null && options.SlidingExpiration is null)
            absolute = _options.DefaultExpiration;
        if (isNull) absolute = absolute is { } limit && limit < _options.NullExpiration ? limit : _options.NullExpiration;
        return new(absolute, options.SlidingExpiration);
    }

    private string FullKey(string key)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var full = _options.KeyPrefix + key;
        if (full.Length > 1024) throw new ArgumentException("The prefixed key must be at most 1024 characters.", nameof(key));
        return full;
    }

    private static string TypeName<T>() => typeof(T).Assembly.GetName().Name + ":" + typeof(T);
    private SemaphoreSlim Gate(string key) => _writeGates[(uint)StringComparer.Ordinal.GetHashCode(key) % (uint)_writeGates.Length];

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0) _shutdown.Cancel();
    }

    private sealed class PendingFill
    {
        internal Type ValueType { get; }
        internal Lazy<Task<object?>> Task { get; }
        internal bool Invalidated { get; set; }

        internal PendingFill(Type valueType, Func<Task<object?>> factory)
        {
            ValueType = valueType;
            Task = new(() =>
            {
                var task = factory();
                // Observe faults even when every caller has cancelled their wait.
                _ = task.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                return task;
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}
