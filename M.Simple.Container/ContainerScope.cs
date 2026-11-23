using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Container;

internal sealed class ContainerScope(SimpleServiceProvider owner, bool isRoot)
    : IServiceScope, IServiceProvider, IKeyedServiceProvider, IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<int, object?> _instances = [];
    private readonly List<object> _disposables = [];
    private readonly HashSet<object> _tracked = new(ReferenceEqualityComparer.Instance);
    private volatile bool _disposed;
    private Task? _disposeTask;

    public bool IsRoot { get; } = isRoot;
    public bool IsDisposed => _disposed;
    public IServiceProvider ServiceProvider => this;

    public object? GetService(Type serviceType) => GetKeyedService(serviceType, null);

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        lock (_gate)
        {
            CheckAlive();
            return owner.Resolve(this, serviceType, serviceKey);
        }
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        => GetKeyedService(serviceType, serviceKey) ?? throw SimpleServiceProvider.Missing(serviceType, serviceKey);

    internal object? GetOrCreate(int slot, Func<ContainerScope, object?> create)
    {
        lock (_gate)
        {
            CheckAlive();
            if (_instances.TryGetValue(slot, out var instance)) return instance;
            instance = Capture(create(this));
            _instances.Add(slot, instance);
            return instance;
        }
    }

    internal object? Capture(object? instance)
    {
        // All callers hold this scope's gate (directly or via GetOrCreate).
        if (_disposed)
        {
            // A user factory can dispose its own provider while its creation is in progress.
            if (instance is IDisposable disposable) disposable.Dispose();
            else if (instance is IAsyncDisposable asyncDisposable) asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            throw new ObjectDisposedException(nameof(ContainerScope));
        }
        if (instance is IDisposable or IAsyncDisposable && !ReferenceEquals(instance, this)
            && !ReferenceEquals(instance, owner) && _tracked.Add(instance)) _disposables.Add(instance);
        return instance;
    }

    private void CheckAlive()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        owner.ThrowIfDisposed();
    }

    public void Dispose()
    {
        object[] instances;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            instances = TakeDisposables();
        }
        List<Exception>? errors = null;
        for (var i = instances.Length - 1; i >= 0; i--)
        {
            var instance = instances[i];
            try
            {
                if (instance is IDisposable disposable) disposable.Dispose();
                else throw new InvalidOperationException($"'{instance.GetType()}' only supports async disposal. Use await using / DisposeAsync().");
            }
            catch (Exception ex) { (errors ??= []).Add(ex); }
        }
        if (errors is not null) throw new AggregateException("Errors disposing container scope.", errors);
    }

    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (_disposeTask is not null) return new ValueTask(_disposeTask);
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;
            _disposeTask = DisposeInstancesAsync(TakeDisposables());
            return new ValueTask(_disposeTask);
        }
    }

    private object[] TakeDisposables()
    {
        var instances = _disposables.ToArray();
        _disposables.Clear();
        _tracked.Clear();
        _instances.Clear();
        return instances;
    }

    private static async Task DisposeInstancesAsync(object[] instances)
    {
        List<Exception>? errors = null;
        for (var i = instances.Length - 1; i >= 0; i--)
        {
            var instance = instances[i];
            try
            {
                if (instance is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                else ((IDisposable)instance).Dispose();
            }
            catch (Exception ex) { (errors ??= []).Add(ex); }
        }
        if (errors is not null) throw new AggregateException("Errors disposing container scope.", errors);
    }
}
