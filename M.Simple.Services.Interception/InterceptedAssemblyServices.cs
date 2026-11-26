using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Services.Interception;

public static class InterceptedAssemblyServices
{
    /// <summary>Scans interfaces as Scoped services, using proxies only for contracts with hook markers.</summary>
    public static IServiceCollection AddInterceptedAssemblyServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);
        if (assemblies.Length == 0 || assemblies.Any(a => a is null))
            throw new ArgumentException("Specify at least one non-null assembly.", nameof(assemblies));
        // Stage registrations so invalid signatures do not leave a half-registered scan.
        var staged = new ServiceCollection();
        foreach (var descriptor in services) ((IServiceCollection)staged).Add(descriptor);
        foreach (var implementation in assemblies.Distinct().SelectMany(a => a.GetTypes()).Distinct().OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            if (!implementation.IsClass || implementation.IsAbstract || implementation.IsNestedPrivate
                || implementation.IsDefined(typeof(CompilerGeneratedAttribute), false)) continue;
            foreach (var contract in implementation.GetInterfaces().Where(t => t != typeof(IDisposable) && t != typeof(IAsyncDisposable)))
            {
                var intercepted = HasHooks(contract, implementation);
                if (implementation.ContainsGenericParameters)
                {
                    if (intercepted)
                        throw new NotSupportedException($"Open generic interception {contract} -> {implementation} requires explicit closed service registration.");
                    if (!contract.IsGenericType || !contract.GetGenericArguments().SequenceEqual(implementation.GetGenericArguments()))
                        throw new InvalidOperationException($"Unsupported open generic mapping {contract} -> {implementation}.");
                    AddPlain(staged, contract.GetGenericTypeDefinition(), implementation);
                    continue;
                }
                if (staged.OfType<ScanMarker>().Any(d => d.ServiceType == contract && d.Implementation == implementation)) continue;
                if (!intercepted) { AddPlain(staged, contract, implementation); continue; }
                if (staged.Any(d => !d.IsKeyedService && d.ServiceType == contract && d.ImplementationType == implementation))
                    throw new InvalidOperationException($"{contract} -> {implementation} is already registered without interception. Use AddInterceptedAssemblyServices instead of scanning the same assembly twice.");
                try
                {
                    RegisterMethod.MakeGenericMethod(contract, implementation).Invoke(null, new object[] { staged, services });
                }
                catch (TargetInvocationException error) when (error.InnerException is not null)
                {
                    ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                }
                var proxy = staged.Last();
                staged.RemoveAt(staged.Count - 1);
                ((IServiceCollection)staged).Add(new ScanMarker(contract, implementation, proxy.ImplementationFactory!));
            }
        }
        foreach (var descriptor in staged.Skip(services.Count)) services.Add(descriptor);
        return services;
    }

    private static readonly MethodInfo RegisterMethod = typeof(InterceptedAssemblyServices)
        .GetMethod(nameof(Register), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static void Register<TService, TImplementation>(IServiceCollection services, IServiceCollection validationServices)
        where TService : class where TImplementation : class, TService
        => InterceptionServiceCollectionExtensions.AddNamed<TService, TImplementation>(services, ServiceLifetime.Scoped, true, validationServices);

    private static void AddPlain(IServiceCollection services, Type contract, Type implementation)
    {
        if (!services.Any(d => !d.IsKeyedService && d.ServiceType == contract && d.ImplementationType == implementation))
            services.Add(new ServiceDescriptor(contract, implementation, ServiceLifetime.Scoped));
    }

    private static bool HasHooks(Type contract, Type implementation)
    {
        var types = contract.GetInterfaces().Append(contract).Append(implementation);
        if (types.Any(t => t.IsDefined(typeof(MethodHookAttribute), true))) return true;
        if (implementation.ContainsGenericParameters)
            return types.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                .Any(m => m.IsDefined(typeof(MethodHookAttribute), true) || m.IsDefined(typeof(BeforeMethodAttribute), true)
                    || m.IsDefined(typeof(AfterMethodAttribute), true));
        foreach (var type in contract.GetInterfaces().Append(contract))
        {
            var map = implementation.GetInterfaceMap(type);
            if (map.InterfaceMethods.Concat(map.TargetMethods).Any(m => m.IsDefined(typeof(MethodHookAttribute), true)
                || m.IsDefined(typeof(BeforeMethodAttribute), true) || m.IsDefined(typeof(AfterMethodAttribute), true))) return true;
        }
        return false;
    }

    private sealed class ScanMarker(Type service, Type implementation, Func<IServiceProvider, object> factory)
        : ServiceDescriptor(service, factory, ServiceLifetime.Scoped)
    {
        internal Type Implementation { get; } = implementation;
    }
}
