using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
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
            if (list!=null && list.Count > 0)
            {
                PropertyInfo[] propertys = list[0].GetType().GetProperties();
                foreach (PropertyInfo pi in propertys)
                {
                    //获取类型
                    Type colType = pi.PropertyType;
                    //当类型为Nullable<>时
                    if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                    {
                        colType = colType.GetGenericArguments()[0];
                    }

                    var displayNameAttribute = pi.GetCustomAttribute<DisplayNameAttribute>();//列名
                    if (displayNameAttribute != null)
                    {
                        result.Columns.Add(displayNameAttribute.DisplayName, colType);
                    }
                    else
                    {
                        result.Columns.Add(pi.Name, colType);
                    }
                }
                for (int i = 0; i < list.Count; i++)
                {
                    ArrayList tempList = [];
                    foreach (PropertyInfo pi in propertys)
                    {
                        object obj = pi.GetValue(list[i], null);
                        tempList.Add(obj);
                    }
                    object?[] array = tempList.ToArray();
                    result.LoadDataRow(array, true);
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
    }
}
