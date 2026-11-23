using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Container;

/// <summary>Only type metadata is shared. Resolvers, keys and instances stay with their provider.</summary>
internal static class ConstructorBinding
{
    // Weak keys permit collectible plugin types to unload. Values never capture a provider.
    private static readonly ConditionalWeakTable<Type, CachedConstructor[]> s_cache = new();

    internal static CachedConstructor[] GetConstructors(Type type) => s_cache.GetValue(type, static implementation =>
        implementation.GetConstructors().OrderByDescending(c => c.GetParameters().Length)
            .Select(c => new CachedConstructor(ConstructorInvoker.Create(c), c.GetParameters().Select(p => new CachedParameter(p)).ToArray()))
            .ToArray());

    internal static Func<ContainerScope, object?> Bind(ConstructorInvoker invoker, Func<ContainerScope, object?>[] arguments)
        => arguments.Length switch
        {
            0 => _ => invoker.Invoke(),
            1 => scope => invoker.Invoke(arguments[0](scope)),
            2 => scope => invoker.Invoke(arguments[0](scope), arguments[1](scope)),
            3 => scope => invoker.Invoke(arguments[0](scope), arguments[1](scope), arguments[2](scope)),
            4 => scope => invoker.Invoke(arguments[0](scope), arguments[1](scope), arguments[2](scope), arguments[3](scope)),
            _ => scope => InvokeMany(invoker, arguments, scope)
        };

    private static object InvokeMany(ConstructorInvoker invoker, Func<ContainerScope, object?>[] arguments, ContainerScope scope)
    {
        var values = ArrayPool<object?>.Shared.Rent(arguments.Length);
        try
        {
            for (var i = 0; i < arguments.Length; i++) values[i] = arguments[i](scope);
            return invoker.Invoke(values.AsSpan(0, arguments.Length));
        }
        finally { ArrayPool<object?>.Shared.Return(values, clearArray: true); }
    }
}

internal sealed record CachedConstructor(ConstructorInvoker Invoker, CachedParameter[] Parameters);

internal sealed class CachedParameter
{
    internal Type Type { get; }
    internal string? Name { get; }
    internal object? Key { get; }
    internal bool IsServiceKey { get; }
    internal Func<ContainerScope, object?>? DefaultResolver { get; }

    internal CachedParameter(ParameterInfo parameter)
    {
        Type = parameter.ParameterType;
        Name = parameter.Name;
        Key = parameter.GetCustomAttribute<FromKeyedServicesAttribute>()?.Key;
        IsServiceKey = parameter.IsDefined(typeof(ServiceKeyAttribute));
        if (!parameter.HasDefaultValue) return;
        var value = parameter.DefaultValue;
        if (value is null or DBNull or Missing)
            value = Type.IsValueType && Nullable.GetUnderlyingType(Type) is null
                ? RuntimeHelpers.GetUninitializedObject(Type) : null;
        DefaultResolver = _ => value;
    }
}
