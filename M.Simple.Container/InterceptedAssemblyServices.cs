using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Container;

/// <summary>Assembly registration using the optional Interception runtime.</summary>
public static class InterceptedAssemblyServices
{
    /// <summary>Registers scoped services, selecting proxies for contracts with hook attributes.</summary>
    public static IServiceCollection AddInterceptedAssemblyServices(this IServiceCollection services, params Assembly[] assemblies)
        => services.Scan(assemblies).AsImplementedInterfaces().RegisterHookedScoped();

    /// <summary>Filters by a marker and excludes the marker itself from interface mappings.</summary>
    public static IServiceCollection AddInterceptedAssemblyServices<TMarker>(this IServiceCollection services, params Assembly[] assemblies)
        => services.Scan(assemblies).AssignableTo<TMarker>().ExcludeInterface(typeof(TMarker))
            .AsImplementedInterfaces().RegisterHookedScoped();
}
