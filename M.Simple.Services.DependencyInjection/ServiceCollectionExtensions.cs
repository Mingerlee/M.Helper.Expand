using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace M.Simple.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimpleDependencyInjection(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<AutowiredService>();
        return services;
    }

    /// <summary>Registers attributed concrete classes. Prefer explicit assemblies; no arguments scans currently loaded assemblies.</summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);
        var descriptors = new List<ServiceDescriptor>();
        foreach (var assembly in (assemblies.Length == 0 ? AppDomain.CurrentDomain.GetAssemblies() : assemblies).Distinct())
        {
            ArgumentNullException.ThrowIfNull(assembly);
            foreach (var type in assembly.GetTypes())
            {
                var attribute = type.GetCustomAttribute<AppServiceAttribute>();
                if (attribute is null || !type.IsClass || type.IsAbstract || type.ContainsGenericParameters) continue;
                var serviceType = attribute.ServiceType
                    ?? (attribute.InterfaceServiceType ? type.GetInterfaces().FirstOrDefault() : null)
                    ?? type;
                if (!serviceType.IsAssignableFrom(type))
                    throw new InvalidOperationException($"{type.FullName} cannot implement {serviceType.FullName}.");
                if (!Enum.IsDefined(attribute.Lifetime))
                    throw new InvalidOperationException($"Invalid service lifetime on {type.FullName}.");
                descriptors.Add(new ServiceDescriptor(serviceType, type, attribute.Lifetime));
            }
        }
        services.AddSimpleDependencyInjection();
        foreach (var descriptor in descriptors) services.TryAddEnumerable(descriptor);
        return services;
    }
}
