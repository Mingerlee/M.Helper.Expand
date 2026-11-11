using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>将List数据转换为DataTable</para>
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static DataTable ToDataTable(this IList list)
        {
            DataTable result = new();
            if (list == null) return result;
            // 泛型空集合也保留表结构；非泛型空集合无法推断元素类型。
            Type? itemType = list.GetType().GetInterfaces().Append(list.GetType())
                .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.GetGenericArguments()[0];
            itemType = list.Cast<object?>().FirstOrDefault(item => item != null)?.GetType() ?? itemType;
            if (itemType == null) return result;
            var properties = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0).ToArray();
            foreach (var property in properties)
            {
                Type columnType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (columnType.IsEnum) columnType = Enum.GetUnderlyingType(columnType);
                string displayName = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? property.Name;
                string columnName = string.IsNullOrEmpty(displayName) ? property.Name : displayName;
                // 显示名重复时生成独立列，映射信息记录原属性名，避免丢值或错位。
                if (result.Columns.Contains(columnName)) columnName = property.Name;
                string baseName = columnName;
                for (int suffix = 2; result.Columns.Contains(columnName); suffix++) columnName = baseName + "_" + suffix;
                var column = result.Columns.Add(columnName, columnType);
                column.Caption = displayName;
                column.ExtendedProperties["M.Simple.PropertyName"] = property.Name;
            }
            foreach (var item in list)
            {
                if (item == null) throw new ArgumentException("集合不能包含null元素。", nameof(list));
                var values = properties.Select(p =>
                {
                    object? value = p.GetValue(item);
                    return value is Enum ? Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())) : value ?? DBNull.Value;
                }).ToArray();
                result.Rows.Add(values);
            }
            result.AcceptChanges();
            return result;
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>获取数组内值的HashCode之和</para>
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int GetListHashCode(this IEnumerable<int> list)
        {
            int result = 0;
            foreach (var item in list)
            {
                result += item.GetHashCode();
            }
            return result;
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>获取数组内值的HashCode之和</para>
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int GetListHashCode(this IEnumerable<int?> list)
        {
            int result = 0;
            foreach (var item in list)
            {
                if (item.HasValue)
                {
                    result += item.GetHashCode();
                }
            }
            return result;
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>获取数组内值的HashCode之和</para>
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int GetListHashCode(this IList<int?> list)
        {
            int result = 0;
            foreach (var item in list)
            {
                if (item.HasValue)
                    result += item.GetHashCode();
            }
            return result;
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>获取数组内值的HashCode之和</para>
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int GetListHashCode(this IList<int> list)
        {
            int result = 0;
            foreach (var item in list)
            {
                result += item.GetHashCode();
            }
            return result;
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>获取数组内值的HashCode之和</para>
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int GetListHashCode(this IEnumerable<string> list)
        {
            int result = 0;
            foreach (var item in list)
            {
                result += item.GetHashCode();
            }
            return result;
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>获取数组内值的HashCode之和</para>
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int GetListHashCode(this IList<string> list)
        {
            int result = 0;
            foreach (var item in list)
            {
                result += item.GetHashCode();
            }
            return result;
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>安全判断集合是否为空（null/无元素）</para>
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || !collection.Any();
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>如果集合为 null，返回空集合（避免空引用异常）</para>
        /// </summary>
        public static IEnumerable<T> DefaultEmpty<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>排除指定元素</para>
        /// </summary>
        /// <remarks>默认全属性比较实体，值类型和字符串按自身规则比较；支持自定义比较器，保留结果顺序和重复项。</remarks>
        public static IEnumerable<T> ExceptItem<T>(this IEnumerable<T> source, T item, IEqualityComparer<T>? comparer = null)
        {
            if (source == null) yield break;
            // 保留实体默认全属性比较，由比较器对基础类型和字符串使用安全的相等规则。
            comparer ??= FastAllPropertiesEqualityComparer<T>.Instance;
            foreach (var element in source)
            {
                if (!comparer.Equals(element, item)) yield return element;
            }
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>排除指定的多个元素（支持自定义相等比较）</para>
        /// </summary>
        /// <param name="source">源集合</param>
        /// <param name="itemsToExclude">要排除的元素集合</param>
        /// <param name="comparer">自定义相等比较器</param>
        /// <remarks>默认全属性比较实体，值类型和字符串按自身规则比较；来源与排除集合各最多枚举一次，保留结果顺序和重复项。</remarks>
        public static IEnumerable<T> ExceptItems<T>(this IEnumerable<T> source, IEnumerable<T> itemsToExclude, IEqualityComparer<T>? comparer = null)
        {
            // 只判断null，不使用Any预读，避免一次性数据源被重复枚举。
            if (source == null) yield break;
            if (itemsToExclude == null)
            {
                foreach (var item in source) yield return item;
                yield break;
            }

            // 实体按全部公共可读属性比较，Equals与GetHashCode使用一致的比较范围。
            comparer ??= FastAllPropertiesEqualityComparer<T>.Instance;

            // 将排除集合转为HashSet，提升查找性能（O(1)）
            var excludeSet = new HashSet<T>(itemsToExclude, comparer);

            // 遍历源集合，排除指定元素
            foreach (var item in source)
            {
                if (!excludeSet.Contains(item))
                {
                    yield return item;
                }
            }
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>判断集合长度是否 >= 指定值（无需全遍历，性能优化）</para>
        /// </summary>
        public static bool HasAtLeast<T>(this IEnumerable<T> source, int count)
        {
            if (count <= 0) return true;
            using var enumerator = source.DefaultEmpty().GetEnumerator();
            int i = 0;
            while (enumerator.MoveNext() && ++i < count) ;
            return i >= count;
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>随机获取集合中的一个元素</para>
        /// </summary>
        public static T RandomTake<T>(this IEnumerable<T> source)
        {
            if (source.IsNullOrEmpty())
                return default;

            var list = source.ToList();
            var random = new Random();
            return list[random.Next(list.Count)];
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>分组后取每组的第一条数据</para>
        /// </summary>
        public static IEnumerable<T> GroupBySelectFirst<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
        {
            if (source.IsNullOrEmpty() || keySelector == null)
                return Enumerable.Empty<T>();

            return source.GroupBy(keySelector).Select(g => g.First());
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>分组后排序，取每组排序后的第一条数据</para>
        /// </summary>
        /// <typeparam name="T">源集合元素类型</typeparam>
        /// <typeparam name="TGroupKey">分组字段类型</typeparam>
        /// <typeparam name="TSortKey">排序字段类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="groupKeySelector">分组字段选择器（按什么分组）</param>
        /// <param name="sortKeySelector">排序字段选择器（分组内按什么排序）</param>
        /// <param name="isDescending">是否降序排序（默认true）</param>
        /// <returns>每组排序后的第一条数据组成的集合</returns>
        public static IEnumerable<T> GroupByThenSortTakeFirst<T, TGroupKey, TSortKey>(
            this IEnumerable<T> source,
            Func<T, TGroupKey> groupKeySelector,
            Func<T, TSortKey> sortKeySelector,
            bool isDescending = true) where TSortKey : IComparable<TSortKey>
        {
            // 1. 空值校验：源集合/分组选择器/排序选择器为空时，返回空集合
            if (source == null || groupKeySelector == null || sortKeySelector == null)
            {
                yield break;
            }

            // 2. 分组 → 对每个分组排序 → 取第一条
            foreach (var group in source.GroupBy(groupKeySelector))
            {
                // 跳过空分组（避免排序时抛出异常）
                if (!group.Any())
                {
                    continue;
                }

                // 对分组内元素排序
                IOrderedEnumerable<T> sortedGroup = isDescending
                    ? group.OrderByDescending(sortKeySelector)
                    : group.OrderBy(sortKeySelector);

                // 取排序后的第一条数据并返回
                yield return sortedGroup.First();
            }
        }

        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>按批次处理集合</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="batchSize">每批次大小</param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> SplitBatch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (source.IsNullOrEmpty() || batchSize < 1)
                yield break; // 无数据时直接终止迭代

            List<T> batch = new(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch; // 关键：返回当前批次，暂停执行
                    batch = new List<T>(batchSize); // 重置批次容器
                }
            }

            if (batch.Count != 0)
                yield return batch; // 返回最后一批不足batchSize的元素
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>筛选非空元素（针对可空引用类型/值类型）</para>
        /// </summary>
        ///<remarks>过滤集合中的null元素string,int</remarks>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : struct
        {
            if (source == null) yield break;
            foreach (var item in source)
            {
                if (item.HasValue) yield return item.Value;
            }
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>筛选非空元素（针对可空引用类型/值类型）</para>
        /// </summary>
        ///<remarks>过滤集合中的null元素class</remarks>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source) where T : class
        {
            if (source == null) yield break;
            foreach (var item in source)
            {
                if (item != null) yield return item;
            }
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>扁平化嵌套集合</para>
        /// </summary>
        ///<remarks>如[[1,2],[3,4]] → [1,2,3,4]</remarks>
        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source)
        {
            if (source == null) yield break;
            foreach (var subCollection in source)
            {
                if (subCollection == null) continue;
                foreach (var item in subCollection)
                {
                    yield return item;
                }
            }
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>转换为字典（重复键时取最后一个值）</para>
        /// </summary>
        public static Dictionary<TKey, T> ToDictionarySafe<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        {
            if (source == null || keySelector == null) return new Dictionary<TKey, T>(comparer);

            var dict = new Dictionary<TKey, T>(comparer);
            foreach (var item in source)
            {
                var key = keySelector(item);
                dict[key] = item; // 重复键覆盖
            }
            return dict;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>列表转换成字典(自动忽略重复数据)</para>
        /// </summary>
        /// <typeparam name="TModel">Model的类型</typeparam>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="list">列表</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数</param>
        /// <param name="elementSelector">用于从每个元素产生结果元素值的转换函数</param>
        /// <returns>Value</returns>
        public static Dictionary<TKey, TValue> ToDictionarys<TModel, TKey, TValue>(this IEnumerable<TModel> list, Func<TModel, TKey> keySelector, Func<TModel, TValue> elementSelector) where TKey : notnull
        {
            var dict = new Dictionary<TKey, TValue>();
            if (list != null && list.Any())
            {
                foreach (var model in list)
                {
                    var key = keySelector(model);
                    var value = elementSelector(model);
                    dict.MergeIgnore(key, value);
                }
            }

            return dict;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>随机打乱集合元素顺序</para>
        /// </summary>
        /// <remarks>Fisher-Yates 洗牌算法，高性能</remarks>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            if (source.IsNullOrEmpty()) yield break;
            var list = source.ToList();
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]); // 值交换
            }
            foreach (var item in list) yield return item;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>取集合最后 N 条元素（无需全遍历，性能优化）</para>
        /// </summary>
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
        {
            if (count <= 0) yield break;
            var queue = new Queue<T>(count);

            foreach (var item in source.DefaultEmpty())
            {
                if (queue.Count == count) queue.Dequeue();
                queue.Enqueue(item);
            }

            while (queue.Count > 0)
                yield return queue.Dequeue();
        }
    }
}
