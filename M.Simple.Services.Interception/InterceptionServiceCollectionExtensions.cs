using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

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

    internal static IServiceCollection AddNamed<TService, TImplementation>(IServiceCollection services, ServiceLifetime lifetime, bool attributedOnly,
        IServiceCollection? validationServices = null)
        where TService : class where TImplementation : class, TService
    {
        ArgumentNullException.ThrowIfNull(services);
        MethodHookProxy<TService>.ValidateContract();
        services.TryAdd(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));
        HookRegistration.Validate(services, typeof(TService), typeof(TImplementation), lifetime, requireRegistered: false);
        services.Add(new ServiceDescriptor(typeof(TService), provider => MethodHookProxy<TService>.Create(
            ResolveTarget<TService, TImplementation>(validationServices ?? services, provider, lifetime), attributedOnly, provider,
            provider.GetService<ILoggerFactory>()?.CreateLogger("M.Simple.Services.Interception")), lifetime));
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
        HookRegistration.Validate(services, typeof(TService), typeof(TImplementation), lifetime, typeof(TInterceptor), false);
        services.Add(new ServiceDescriptor(typeof(TService), provider => MethodHookProxy<TService>.Create(
            ResolveTarget<TService, TImplementation>(services, provider, lifetime, typeof(TInterceptor)),
            provider.GetRequiredService<TInterceptor>(), attributedOnly, provider,
            provider.GetService<ILoggerFactory>()?.CreateLogger("M.Simple.Services.Interception")), lifetime));
        return services;
    }

    private static TImplementation ResolveTarget<TService, TImplementation>(IServiceCollection services,
        IServiceProvider provider, ServiceLifetime lifetime, Type? interceptor = null)
        where TService : class where TImplementation : class, TService
    {
        HookRegistration.Validate(services, typeof(TService), typeof(TImplementation), lifetime, interceptor);
        return provider.GetRequiredService<TImplementation>();
    }
}
