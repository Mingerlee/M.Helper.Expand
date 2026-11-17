using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace M.Simple.Caching;

public static class SimpleCacheServiceCollectionExtensions
{
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
        services.AddSingleton<ISimpleCache, SimpleCache>();
        return services;
    }
}
