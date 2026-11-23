using System.Runtime.CompilerServices;

namespace M.Simple.Container;

internal static class EnumerableBinding
{
    private static readonly ConditionalWeakTable<Type, IArrayFactory> s_cache = new();

    internal static Func<ContainerScope, object?> Bind(Type item, Func<ContainerScope, object?>[] resolvers)
        => s_cache.GetValue(item, static type => (IArrayFactory)Activator.CreateInstance(
            typeof(ArrayFactory<>).MakeGenericType(type), nonPublic: true)!).Bind(resolvers);

    private interface IArrayFactory
    {
        Func<ContainerScope, object?> Bind(Func<ContainerScope, object?>[] resolvers);
    }

    private sealed class ArrayFactory<T> : IArrayFactory
    {
        public Func<ContainerScope, object?> Bind(Func<ContainerScope, object?>[] resolvers)
        {
            if (resolvers.Length == 0) return _ => Array.Empty<T>();
            return scope =>
            {
                var array = new T[resolvers.Length];
                for (var i = 0; i < resolvers.Length; i++) array[i] = (T)resolvers[i](scope)!;
                return array;
            };
        }
    }
}
