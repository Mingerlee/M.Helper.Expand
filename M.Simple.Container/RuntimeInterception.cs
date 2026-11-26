using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Container;

internal static class RuntimeInterception
{
    private const string AssemblyName = "M.Simple.Services.Interception";
    private static readonly Lazy<IReadOnlyDictionary<ServiceLifetime, MethodInfo>> Methods = new(LoadMethods);

    internal static void RegisterProxy(IServiceCollection services, Type contract, Type implementation, ServiceLifetime lifetime)
    {
        try
        {
            Methods.Value[lifetime].MakeGenericMethod(contract, implementation)
                .Invoke(null, new object[] { services, true });
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
        }
    }

    private static IReadOnlyDictionary<ServiceLifetime, MethodInfo> LoadMethods()
    {
        Assembly assembly;
        try { assembly = Assembly.Load(AssemblyName); }
        catch (Exception error) when (error is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            throw new InvalidOperationException(
                "Intercepted registration requires the application to reference M.Simple.Services.Interception (1.0.0.4 or later). Container does not install it automatically.", error);
        }
        var extensions = assembly.GetType(AssemblyName + ".InterceptionServiceCollectionExtensions");
        var methods = new Dictionary<ServiceLifetime, MethodInfo>();
        foreach (var lifetime in Enum.GetValues<ServiceLifetime>())
        {
            var name = "AddIntercepted" + lifetime;
            var method = extensions?.GetMethods(BindingFlags.Public | BindingFlags.Static).SingleOrDefault(m =>
                m.Name == name && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 2
                && m.GetParameters() is { Length: 2 } parameters
                && parameters[0].ParameterType == typeof(IServiceCollection) && parameters[1].ParameterType == typeof(bool));
            if (method is null)
                throw new InvalidOperationException($"The installed {AssemblyName} does not expose {name}<TService, TImplementation>(IServiceCollection, bool). Install a compatible version (1.0.0.4 or later).");
            methods.Add(lifetime, method);
        }
        return methods;
    }
}
