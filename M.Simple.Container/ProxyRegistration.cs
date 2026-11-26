using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Container;

/// <summary>Creates an interface proxy around a DI-owned target. The proxy must not dispose the target.</summary>
public delegate object ServiceProxyFactory(Type serviceType, object target, IServiceProvider serviceProvider);

internal static class ProxyRegistration
{
    internal static IServiceCollection Register(IServiceCollection services, IReadOnlyList<ScannedDescriptor> descriptors,
        ServiceLifetime lifetime, ServiceProxyFactory factory)
    {
        IServiceCollection staged = new ServiceCollection();
        foreach (var descriptor in services) staged.Add(descriptor);
        foreach (var descriptor in descriptors)
        {
            var contract = descriptor.ServiceType;
            var implementation = descriptor.ImplementationType!;
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
                && d.Lifetime == lifetime && d.Factory == factory)) continue;
            foreach (var previous in staged.Where(d => !d.IsKeyedService && d.ServiceType == contract
                && (d.ImplementationType == implementation || d is ProxyDescriptor proxy && proxy.Implementation == implementation)).ToArray())
                staged.Remove(previous);
            if (targetRegistration is null)
                staged.Add(new ScannedDescriptor(implementation, implementation, lifetime, descriptor.Identity));
            staged.Add(new ProxyDescriptor(contract, implementation, factory, lifetime));
        }
        // Commit only after every mapping has passed validation.
        services.Clear();
        foreach (var descriptor in staged) services.Add(descriptor);
        return services;
    }

    private sealed class ProxyDescriptor(Type service, Type implementation, ServiceProxyFactory factory, ServiceLifetime lifetime)
        : ServiceDescriptor(service, provider => Create(service, implementation, factory, provider), lifetime)
    {
        internal Type Implementation { get; } = implementation;
        internal ServiceProxyFactory Factory { get; } = factory;
    }

    private static object Create(Type contract, Type implementation, ServiceProxyFactory factory, IServiceProvider provider)
    {
        var target = provider.GetRequiredService(implementation);
        var proxy = factory(contract, target, provider);
        if (proxy is null || !contract.IsInstanceOfType(proxy) || ReferenceEquals(proxy, target))
            throw new InvalidOperationException($"Proxy factory for {contract} -> {implementation} must return a distinct object implementing {contract}.");
        return proxy;
    }
}
