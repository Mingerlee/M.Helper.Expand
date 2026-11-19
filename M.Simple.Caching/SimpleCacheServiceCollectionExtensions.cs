using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace M.Simple.Caching;

public static class SimpleCacheServiceCollectionExtensions
{
    /// <summary>Configures read-only native data access separately from dashboard permissions.</summary>
    public static IServiceCollection AddSimpleNativeCache(this IServiceCollection services, Action<SimpleNativeCacheOptions>? configure = null)
    {
        var options = new SimpleNativeCacheOptions();
        configure?.Invoke(options);
        services.Replace(ServiceDescriptor.Singleton(options.Snapshot()));
        return services;
    }

    /// <summary>Configures administration without changing business cache keys.</summary>
    public static IServiceCollection AddSimpleCacheManagement(this IServiceCollection services, Action<SimpleCacheManagementOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new SimpleCacheManagementOptions();
        configure(options);
        services.Replace(ServiceDescriptor.Singleton(options.Snapshot()));
        return services;
    }

    public static IServiceCollection AddSimpleMemoryCache(this IServiceCollection services, Action<SimpleMemoryCacheOptions>? configure = null)
    {
        var options = new SimpleMemoryCacheOptions();
        configure?.Invoke(options);
        return services.AddSimpleCacheStore(options, sp => new MemoryCacheStore(options, sp.GetRequiredService<TimeProvider>()));
    }

    /// <summary>Registers one backend. Redis/MongoDB registration methods call this automatically.</summary>
    public static IServiceCollection AddSimpleCacheStore<TStore>(this IServiceCollection services, SimpleCacheOptions options,
        Func<IServiceProvider, TStore> factory) where TStore : class, ISimpleCacheStore
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(factory);
        options.Validate();
        if (services.Any(service => service.ServiceType == typeof(ISimpleCache)))
            throw new InvalidOperationException("Register only one SimpleCache backend per service collection.");
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton(options.Snapshot());
        services.AddSingleton<ISimpleCacheStore>(sp => factory(sp));
        services.AddSingleton<SimpleCache>();
        services.AddSingleton<ISimpleCache>(sp => sp.GetRequiredService<SimpleCache>());
        services.TryAddSingleton<SimpleCacheManagementOptions>();
        services.TryAddSingleton<SimpleNativeCacheOptions>();
        services.AddSingleton<ISimpleNativeCache, SimpleNativeCache>();
        services.AddSingleton<ISimpleCacheAdmin, SimpleCacheAdmin>();
        return services;
    }
}
