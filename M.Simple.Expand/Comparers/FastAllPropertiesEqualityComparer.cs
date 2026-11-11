using System.Linq.Expressions;
using System.Reflection;

namespace M.Simple.Expand
{
    /// <summary>
    /// 通过预编译委托比较实体的全部公共可读非索引属性；值类型、字符串及无可比较属性的类型使用自身相等规则。
    /// </summary>
    public class FastAllPropertiesEqualityComparer<T> : IEqualityComparer<T>
    {
        // 缓存：属性 → 获取属性值的委托
        private static readonly Dictionary<PropertyInfo, Func<T, object>> _propertyGetters = [];
        // 值类型和字符串按自身规则比较；实体仅比较公共可读的非索引属性。
        private static readonly PropertyInfo[] _properties = typeof(T).IsValueType || typeof(T) == typeof(string)
            ? []
            : typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0)
                .ToArray();

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
            // 无可比较属性时回退类型自身的相等规则，避免把不同基础值或空实体误判为相等。
            if (_properties.Length == 0) return EqualityComparer<T>.Default.Equals(x, y);
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
            // 哈希规则必须与Equals的回退规则保持一致。
            if (_properties.Length == 0) return EqualityComparer<T>.Default.GetHashCode(obj);

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
