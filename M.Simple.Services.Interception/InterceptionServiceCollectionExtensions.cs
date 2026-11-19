using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace M.Simple.Services.Interception;

public static class InterceptionServiceCollectionExtensions
{
    /// <summary>Registers attribute-driven hooks without requiring a separate interceptor.</summary>
    public static IServiceCollection AddInterceptedScoped<TService, TImplementation>(this IServiceCollection services, bool attributedOnly = true)
        where TService : class where TImplementation : class, TService
        => AddNamed<TService, TImplementation>(services, ServiceLifetime.Scoped, attributedOnly);

    public static IServiceCollection AddInterceptedTransient<TService, TImplementation>(this IServiceCollection services, bool attributedOnly = true)
        where TService : class where TImplementation : class, TService
        => AddNamed<TService, TImplementation>(services, ServiceLifetime.Transient, attributedOnly);

    public static IServiceCollection AddInterceptedSingleton<TService, TImplementation>(this IServiceCollection services, bool attributedOnly = true)
        where TService : class where TImplementation : class, TService
        => AddNamed<TService, TImplementation>(services, ServiceLifetime.Singleton, attributedOnly);

    private static IServiceCollection AddNamed<TService, TImplementation>(IServiceCollection services, ServiceLifetime lifetime, bool attributedOnly)
        where TService : class where TImplementation : class, TService
    {
        ArgumentNullException.ThrowIfNull(services);
        MethodHookProxy<TService>.ValidateContract();
        services.TryAdd(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));
        services.Add(new ServiceDescriptor(typeof(TService), provider => MethodHookProxy<TService>.Create(
            provider.GetRequiredService<TImplementation>(), attributedOnly), lifetime));
        return services;
    }

    public static IServiceCollection AddInterceptedScoped<TService, TImplementation, TInterceptor>(
        this IServiceCollection services, bool attributedOnly = false)
        where TService : class where TImplementation : class, TService where TInterceptor : MethodInterceptor
        => AddIntercepted<TService, TImplementation, TInterceptor>(services, ServiceLifetime.Scoped, attributedOnly);

    public static IServiceCollection AddInterceptedTransient<TService, TImplementation, TInterceptor>(
        this IServiceCollection services, bool attributedOnly = false)
        where TService : class where TImplementation : class, TService where TInterceptor : MethodInterceptor
        => AddIntercepted<TService, TImplementation, TInterceptor>(services, ServiceLifetime.Transient, attributedOnly);

    public static IServiceCollection AddInterceptedSingleton<TService, TImplementation, TInterceptor>(
        this IServiceCollection services, bool attributedOnly = false)
        where TService : class where TImplementation : class, TService where TInterceptor : MethodInterceptor
        => AddIntercepted<TService, TImplementation, TInterceptor>(services, ServiceLifetime.Singleton, attributedOnly);

    private static IServiceCollection AddIntercepted<TService, TImplementation, TInterceptor>(IServiceCollection services,
        ServiceLifetime lifetime, bool attributedOnly)
        where TService : class where TImplementation : class, TService where TInterceptor : MethodInterceptor
    {
        ArgumentNullException.ThrowIfNull(services);
        MethodHookProxy<TService>.ValidateContract();
        services.TryAdd(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(TInterceptor), typeof(TInterceptor), lifetime));
        services.Add(new ServiceDescriptor(typeof(TService), provider => MethodHookProxy<TService>.Create(
            provider.GetRequiredService<TImplementation>(), provider.GetRequiredService<TInterceptor>(), attributedOnly), lifetime));
        return services;
    }
}
