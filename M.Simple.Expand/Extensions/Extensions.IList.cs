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
    }
}
