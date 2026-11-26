using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Services.Interception;

internal static class HookRegistration
{
    internal static IEnumerable<Type> ExternalTypes(Type service, Type implementation)
    {
        foreach (var contract in service.GetInterfaces().Append(service))
        {
            var map = implementation.GetInterfaceMap(contract);
            for (var i = 0; i < map.InterfaceMethods.Length; i++)
            {
                var method = map.InterfaceMethods[i];
                var target = map.TargetMethods[i];
                if (method.IsDefined(typeof(SkipMethodHookAttribute), true) || target.IsDefined(typeof(SkipMethodHookAttribute), true)) continue;
                var before = target.GetCustomAttribute<BeforeMethodAttribute>(true) ?? method.GetCustomAttribute<BeforeMethodAttribute>(true);
                var after = target.GetCustomAttribute<AfterMethodAttribute>(true) ?? method.GetCustomAttribute<AfterMethodAttribute>(true);
                if (before?.HookType is { } beforeType) yield return beforeType;
                if (after?.HookType is { } afterType) yield return afterType;
            }
        }
    }

    internal static void Validate(IServiceCollection services, Type service, Type implementation,
        ServiceLifetime lifetime, Type? interceptor = null, bool requireRegistered = true)
    {
        var hooks = ExternalTypes(service, implementation).Distinct().ToArray();
        foreach (var hook in hooks)
        {
            if (!services.Any(d => !d.IsKeyedService && d.ServiceType == hook) && requireRegistered)
                throw new InvalidOperationException($"Service {service.FullName} -> {implementation.FullName}: external hook {hook.FullName} is not registered. Register it with AddScoped/AddTransient/AddSingleton.");
        }
        // Factory dependencies are checked by the provider at resolution; reflection can inspect typed registrations.
        foreach (var dependency in hooks.Append(implementation).Concat(interceptor is null ? [] : new[] { interceptor }))
            Visit(dependency, lifetime == ServiceLifetime.Singleton, new HashSet<Type>(), $"{service.FullName} -> {implementation.FullName}");

        void Visit(Type type, bool singleton, HashSet<Type> visiting, string chain)
        {
            if (!visiting.Add(type)) return;
            try
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    foreach (var descriptor in services.Where(d => !d.IsKeyedService && d.ServiceType == type.GetGenericArguments()[0]))
                        Inspect(descriptor, singleton, visiting, chain, type.GetGenericArguments()[0]);
                    return;
                }
                var registration = services.LastOrDefault(d => !d.IsKeyedService && (d.ServiceType == type
                    || type.IsConstructedGenericType && d.ServiceType == type.GetGenericTypeDefinition()));
                if (registration is not null) Inspect(registration, singleton, visiting, chain, type);
            }
            finally { visiting.Remove(type); }
        }

        void Inspect(ServiceDescriptor descriptor, bool singleton, HashSet<Type> visiting, string chain, Type requestedType)
        {
            var next = chain + " -> " + descriptor.ServiceType.FullName + $" ({descriptor.Lifetime})";
            if (singleton && descriptor.Lifetime == ServiceLifetime.Scoped)
                throw new InvalidOperationException($"Singleton interception service cannot capture a Scoped dependency: {next}. Change the service/hook lifetime or create an explicit scope.");
            singleton |= descriptor.Lifetime == ServiceLifetime.Singleton;
            var type = descriptor.ImplementationType;
            if (type is null) return;
            if (type.IsGenericTypeDefinition && requestedType.IsConstructedGenericType)
                type = type.MakeGenericType(requestedType.GetGenericArguments());
            if (type.ContainsGenericParameters) return;
            // Check the longest resolvable constructor, matching normal DI constructor selection.
            var constructor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault(c => c.GetParameters().All(p => p.HasDefaultValue
                    || p.ParameterType == typeof(IServiceProvider)
                    || p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    || services.Any(d => !d.IsKeyedService && (d.ServiceType == p.ParameterType
                        || p.ParameterType.IsConstructedGenericType && d.ServiceType == p.ParameterType.GetGenericTypeDefinition()))));
            if (constructor is null) return;
            foreach (var parameter in constructor.GetParameters()) Visit(parameter.ParameterType, singleton, visiting, next);
        }
    }
}
