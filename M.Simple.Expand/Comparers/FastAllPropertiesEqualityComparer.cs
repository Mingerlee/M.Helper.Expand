using System.Linq.Expressions;
using System.Reflection;

namespace M.Simple.Expand
{
    /// <summary>
    /// 极致优化版：预编译委托获取属性值（替代反射）
    /// </summary>
    public class FastAllPropertiesEqualityComparer<T> : IEqualityComparer<T>
    {
        // 缓存：属性 → 获取属性值的委托
        private static readonly Dictionary<PropertyInfo, Func<T, object>> _propertyGetters = [];
        private static readonly PropertyInfo[] _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        static FastAllPropertiesEqualityComparer()
        {
            // 预编译委托（仅初始化1次）
            foreach (var prop in _properties)
            {
                var parameter = Expression.Parameter(typeof(T), "obj");
                var propertyAccess = Expression.Property(parameter, prop);
                var convert = Expression.Convert(propertyAccess, typeof(object));
                var getter = Expression.Lambda<Func<T, object>>(convert, parameter).Compile();
                _propertyGetters[prop] = getter;
            }
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(T? x, T? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            if (ReferenceEquals(x, y)) return true;

            foreach (var prop in _properties)
            {
                // 用预编译委托获取值（替代prop.GetValue，无反射开销）
                var valueX = _propertyGetters[prop](x);
                var valueY = _propertyGetters[prop](y);
                if (!EqualityComparer<object>.Default.Equals(valueX, valueY))
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(T obj)
        {
            if (obj == null) return 0;

            // 组合所有属性的哈希码（避免哈希冲突）
            int hash = 17;
            foreach (var prop in _properties)
            {
                var value = prop.GetValue(obj);
                hash = hash * 31 + (value?.GetHashCode() ?? 0);
            }
            return hash;
        }

        /// <summary>
        ///  
        /// </summary>
        public static readonly FastAllPropertiesEqualityComparer<T> Instance = new();
    }
}
