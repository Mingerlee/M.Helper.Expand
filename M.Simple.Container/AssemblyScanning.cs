using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Container;

public static class AssemblyScanningExtensions
{
    /// <summary>Registers concrete types as their interfaces, scoped by default.</summary>
    public static IServiceCollection AddAssemblyServices(this IServiceCollection services, params Assembly[] assemblies)
        => services.Scan(assemblies).AsImplementedInterfaces().Scoped();

    /// <summary>Registers implementations of a marker, excluding the marker itself from the service mappings.</summary>
    public static IServiceCollection AddAssemblyServices<TMarker>(this IServiceCollection services, params Assembly[] assemblies)
        => services.Scan(assemblies).AssignableTo<TMarker>().ExcludeInterface(typeof(TMarker))
            .AsImplementedInterfaces().Scoped();

    /// <summary>Starts an explicit assembly scan. A terminal lifetime method applies the registrations.</summary>
    public static AssemblyRegistrationBuilder Scan(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);
        if (assemblies.Length == 0) throw new ArgumentException("Specify at least one assembly.", nameof(assemblies));
        if (assemblies.Any(a => a is null)) throw new ArgumentException("Assemblies cannot contain null.", nameof(assemblies));
        return new AssemblyRegistrationBuilder(services, assemblies.Distinct().ToArray());
    }
}

/// <summary>Filters types before registering them. Scans never run during service resolution.</summary>
public sealed class AssemblyRegistrationBuilder
{
    private readonly IServiceCollection _services;
    private readonly Assembly[] _assemblies;
    private readonly List<Func<Type, bool>> _filters = [];
    private readonly HashSet<Type> _excluded = [typeof(IDisposable), typeof(IAsyncDisposable)];
    private bool _interfaces = true;
    private bool _self;
    private readonly HashSet<Type> _contracts = [];
    private ServiceProxyFactory? _proxyFactory;
    private bool _intercepted;
    private bool _onlyHooked;

    internal AssemblyRegistrationBuilder(IServiceCollection services, Assembly[] assemblies)
        => (_services, _assemblies) = (services, assemblies);

    public AssemblyRegistrationBuilder AssignableTo<T>() => AssignableTo(typeof(T));

    public AssemblyRegistrationBuilder AssignableTo(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return Where(type => serviceType.IsGenericTypeDefinition
            ? type.GetInterfaces().Concat(Ancestors(type)).Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == serviceType)
            : serviceType.IsAssignableFrom(type));
    }

    public AssemblyRegistrationBuilder Where(Func<Type, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _filters.Add(predicate);
        return this;
    }

    public AssemblyRegistrationBuilder ExcludeInterface(Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(interfaceType);
        _excluded.Add(interfaceType);
        return this;
    }

    public AssemblyRegistrationBuilder AsImplementedInterfaces() { _interfaces = true; return this; }

    /// <summary>Exposes only the specified implemented interface. Multiple calls expose multiple interfaces.</summary>
    public AssemblyRegistrationBuilder As<TService>() => As(typeof(TService));

    public AssemblyRegistrationBuilder As(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        if (!serviceType.IsInterface) throw new ArgumentException("Specify a service interface.", nameof(serviceType));
        _contracts.Add(serviceType);
        return this;
    }

    /// <summary>Uses a caller-supplied factory to wrap each selected interface's real implementation.</summary>
    public AssemblyRegistrationBuilder WithProxy(ServiceProxyFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (_intercepted)
            throw new InvalidOperationException("Choose either Intercepted registration or an explicit proxy factory.");
        _proxyFactory = factory;
        return this;
    }

    /// <summary>Also exposes the implementation type; interfaces remain exposed.</summary>
    public AssemblyRegistrationBuilder AsSelf() { _self = true; return this; }

    public IServiceCollection Scoped() => Register(ServiceLifetime.Scoped);
    public IServiceCollection Singleton() => Register(ServiceLifetime.Singleton);
    public IServiceCollection Transient() => Register(ServiceLifetime.Transient);

    public IServiceCollection ProxyScoped(ServiceProxyFactory factory) => WithProxy(factory).Scoped();

    public IServiceCollection ProxySingleton(ServiceProxyFactory factory) => WithProxy(factory).Singleton();

    public IServiceCollection ProxyTransient(ServiceProxyFactory factory) => WithProxy(factory).Transient();

    /// <summary>Loads the optional Interception runtime and registers attribute-driven scoped proxies.</summary>
    public IServiceCollection InterceptedScoped() => RegisterIntercepted(ServiceLifetime.Scoped);

    public IServiceCollection InterceptedTransient() => RegisterIntercepted(ServiceLifetime.Transient);

    public IServiceCollection InterceptedSingleton() => RegisterIntercepted(ServiceLifetime.Singleton);

    internal IServiceCollection RegisterHookedScoped()
    {
        _onlyHooked = true;
        return RegisterIntercepted(ServiceLifetime.Scoped);
    }

    private IServiceCollection RegisterIntercepted(ServiceLifetime lifetime)
    {
        if (_proxyFactory is not null)
            throw new InvalidOperationException("Choose either Intercepted registration or an explicit proxy factory.");
        _intercepted = true;
        return Register(lifetime);
    }

    private IServiceCollection Register(ServiceLifetime lifetime)
    {
        // Collect before mutating so a failed scan does not leave partial registrations.
        var descriptors = new List<ScannedDescriptor>();
        foreach (var implementation in _assemblies.SelectMany(a => a.GetTypes()).Distinct()
                     .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            if (!implementation.IsClass || implementation.IsAbstract || implementation.IsNestedPrivate
                || implementation.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)
                || _filters.Any(filter => !filter(implementation))) continue;
            var identity = _services.OfType<ScannedDescriptor>()
                .FirstOrDefault(d => d.ImplementationType == implementation && d.Lifetime == lifetime)?.Identity ?? new object();
            IEnumerable<Type> mappings = _interfaces
                ? implementation.GetInterfaces().Where(t => (_contracts.Count == 0 || _contracts.Contains(t)
                    || t.IsGenericType && _contracts.Contains(t.GetGenericTypeDefinition())) && !_excluded.Contains(t)
                    && !(t.IsGenericType && _excluded.Contains(t.GetGenericTypeDefinition())))
                : [];
            if (_self) mappings = mappings.Append(implementation);
            foreach (var mapping in mappings.Distinct())
            {
                var service = mapping;
                if (implementation.IsGenericTypeDefinition)
                {
                    // Microsoft DI's open generic convention: positional type arguments.
                    if (!mapping.IsGenericType || !mapping.GetGenericArguments().SequenceEqual(implementation.GetGenericArguments()))
                        throw new InvalidOperationException($"Cannot scan generic mapping '{mapping}' -> '{implementation}'. Register this mapping explicitly with a factory.");
                    service = mapping.GetGenericTypeDefinition();
                }
                if (!_intercepted && _proxyFactory is null && _services.Concat(descriptors).Any(d => d.ServiceType == service && !d.IsKeyedService
                    && d.ImplementationType == implementation && d.Lifetime == lifetime)) continue;
                descriptors.Add(new ScannedDescriptor(service, implementation, lifetime, identity));
            }
        }
        if (_intercepted)
            return ProxyRegistration.Register(_services, descriptors, lifetime, null, RuntimeInterception.RegisterProxy,
                _onlyHooked ? RuntimeInterception.HasHooks : null);
        if (_proxyFactory is not null)
            return ProxyRegistration.Register(_services, descriptors, lifetime, _proxyFactory);
        foreach (var descriptor in descriptors) _services.Add(descriptor);
        return _services;
    }

    private static IEnumerable<Type> Ancestors(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType) yield return current;
    }
}

// One cache identity for all interfaces of a scanned implementation.
internal sealed class ScannedDescriptor(Type service, Type implementation, ServiceLifetime lifetime, object identity)
    : ServiceDescriptor(service, implementation, lifetime)
{
    public object Identity { get; } = identity;
}
