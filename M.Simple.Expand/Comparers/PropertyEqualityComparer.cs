using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand
{
    /// <summary>
    /// 通用按属性比较的比较器（适用于任何类型按指定属性判断相等）
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <typeparam name="TKey">比较的属性类型</typeparam>
    public class PropertyEqualityComparer<T, TKey> : IEqualityComparer<T>
    {
        private readonly Func<T, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _keyComparer;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="keySelector">要比较的属性（如 u => u.Id）</param>
        /// <param name="keyComparer">属性的比较器（默认EqualityComparer.Default）</param>
        public PropertyEqualityComparer(Func<T, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null)
        {
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(T? x, T? y)
        {
            // 处理null情况
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            // 比较指定属性
            var xKey = _keySelector(x);
            var yKey = _keySelector(y);
            return _keyComparer.Equals(xKey, yKey);
        }
        /// <summary>
        ///    
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(T obj)
        {
            if (obj == null) return 0;
            var key = _keySelector(obj);
            if (key != null)
                return _keyComparer.GetHashCode(key);

            return 0;
        }
    }
    /// <summary>
    ///     
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// 创建按属性比较的比较器
        /// </summary>
        public static IEqualityComparer<T> ByProperty<T, TKey>(this T _, Func<T, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null)
        {
            return new PropertyEqualityComparer<T, TKey>(keySelector, keyComparer);
        }

        /// <summary>
        ///     创建按属性比较的比较器
        /// </summary>
        /// <returns></returns>
        public static IEqualityComparer<T> CreateByProperty<T, TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null)
        {
            return new PropertyEqualityComparer<T, TKey>(keySelector, keyComparer);
        }
    }
}
