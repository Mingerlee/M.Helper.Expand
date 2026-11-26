using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Container;

/// <summary>Creates an interface proxy around a DI-owned target. The proxy must not dispose the target.</summary>
public delegate object ServiceProxyFactory(Type serviceType, object target, IServiceProvider serviceProvider);

internal static class ProxyRegistration
{
    internal static IServiceCollection Register(IServiceCollection services, IReadOnlyList<ScannedDescriptor> descriptors,
        ServiceLifetime lifetime, ServiceProxyFactory? factory,
        Action<IServiceCollection, Type, Type, ServiceLifetime>? register = null,
        Func<Type, Type, bool>? shouldIntercept = null)
    {
        var staged = new StagedServices(services);
        var key = (Delegate?)factory ?? register ?? throw new ArgumentNullException(nameof(factory));
        foreach (var descriptor in descriptors)
        {
            var contract = descriptor.ServiceType;
            var implementation = descriptor.ImplementationType!;
            if (shouldIntercept is not null && !shouldIntercept(contract, implementation))
            {
                if (!staged.Any(d => !d.IsKeyedService && d.ServiceType == contract && d.Lifetime == lifetime
                    && (d.ImplementationType == implementation || d is ProxyDescriptor proxy && proxy.Implementation == implementation)))
                    staged.Add(descriptor);
                continue;
            }
            if (implementation.ContainsGenericParameters)
                throw new NotSupportedException($"Proxy scanning requires closed types: {contract} -> {implementation}.");
            if (!contract.IsInterface)
            {
                if (!staged.Any(d => !d.IsKeyedService && d.ServiceType == contract)) staged.Add(descriptor);
                continue;
            }
            if (typeof(IDisposable).IsAssignableFrom(contract) || typeof(IAsyncDisposable).IsAssignableFrom(contract))
                throw new NotSupportedException($"Keep disposal on the implementation, not the proxy interface: {contract}.");
            var targetRegistration = staged.LastOrDefault(d => !d.IsKeyedService && d.ServiceType == implementation);
            if (targetRegistration is not null && targetRegistration.Lifetime != lifetime)
                throw new InvalidOperationException($"Proxy {contract} and target {implementation} must have the same lifetime ({lifetime}).");
            if (staged.OfType<ProxyDescriptor>().Any(d => d.ServiceType == contract && d.Implementation == implementation
                && d.Lifetime == lifetime && d.RegistrationKey.Equals(key))) continue;
            foreach (var previous in staged.Where(d => !d.IsKeyedService && d.ServiceType == contract
                && (d.ImplementationType == implementation || d is ProxyDescriptor proxy && proxy.Implementation == implementation)).ToArray())
                staged.Remove(previous);
            if (targetRegistration is null)
                staged.Add(new ScannedDescriptor(implementation, implementation, lifetime, descriptor.Identity));
            Func<IServiceProvider, object> create;
            if (register is not null)
            {
                register(staged, contract, implementation, lifetime);
                var registration = staged[staged.Count - 1];
                staged.RemoveAt(staged.Count - 1);
                create = registration.ImplementationFactory!;
            }
            else create = provider => Create(contract, implementation, factory!, provider);
            staged.Add(new ProxyDescriptor(contract, implementation, create, lifetime, key));
        }
        // Commit only after every mapping has passed validation.
        staged.Commit(services);
        return services;
    }

    private sealed class ProxyDescriptor(Type service, Type implementation, Func<IServiceProvider, object> factory,
        ServiceLifetime lifetime, Delegate key) : ServiceDescriptor(service, factory, lifetime)
    {
        internal Type Implementation { get; } = implementation;
        internal Delegate RegistrationKey { get; } = key;
    }

    private static object Create(Type contract, Type implementation, ServiceProxyFactory factory, IServiceProvider provider)
    {
        var target = provider.GetRequiredService(implementation);
        var proxy = factory(contract, target, provider);
        if (proxy is null || !contract.IsInstanceOfType(proxy) || ReferenceEquals(proxy, target))
            throw new InvalidOperationException($"Proxy factory for {contract} -> {implementation} must return a distinct object implementing {contract}.");
        return proxy;
    }

    // Interception captures this collection for resolution-time validation. After commit it must
    // observe registrations subsequently added to the caller's collection, including external hooks.
    private sealed class StagedServices(IServiceCollection source) : IServiceCollection
    {
        private IList<ServiceDescriptor> _items = source.ToList();
        internal void Commit(IServiceCollection destination)
        {
            destination.Clear();
            foreach (var item in _items) destination.Add(item);
            _items = destination;
        }
        public ServiceDescriptor this[int index] { get => _items[index]; set => _items[index] = value; }
        public int Count => _items.Count;
        public bool IsReadOnly => _items.IsReadOnly;
        public void Add(ServiceDescriptor item) => _items.Add(item);
        public void Clear() => _items.Clear();
        public bool Contains(ServiceDescriptor item) => _items.Contains(item);
        public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public IEnumerator<ServiceDescriptor> GetEnumerator() => _items.GetEnumerator();
        public int IndexOf(ServiceDescriptor item) => _items.IndexOf(item);
        public void Insert(int index, ServiceDescriptor item) => _items.Insert(index, item);
        public bool Remove(ServiceDescriptor item) => _items.Remove(item);
        public void RemoveAt(int index) => _items.RemoveAt(index);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
