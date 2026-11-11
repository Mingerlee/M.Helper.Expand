using System.ComponentModel;
using System.Globalization;
using System.Data;
using System.Reflection;

namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>将DataTable数据转换成List</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static List<T> ToLists<T>(this DataTable dt) where T : class, new()
        {
            List<T> result = [];
            if (dt == null) return result;
            var mappings = new List<(PropertyInfo Property, DataColumn Column)>();
            foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.SetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0))
            {
                // 优先读取原属性映射，兼容普通属性名列和手工创建的DisplayName列。
                var columns = dt.Columns.Cast<DataColumn>();
                var column = columns.FirstOrDefault(c => Equals(c.ExtendedProperties["M.Simple.PropertyName"], property.Name));
                column ??= columns.FirstOrDefault(c => !c.ExtendedProperties.ContainsKey("M.Simple.PropertyName") && c.ColumnName == property.Name);
                string? displayName = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
                column ??= columns.FirstOrDefault(c => !c.ExtendedProperties.ContainsKey("M.Simple.PropertyName") && c.ColumnName == displayName);
                if (column != null) mappings.Add((property, column));
            }
            foreach (DataRow row in dt.Rows)
            {
                T item = new();
                foreach (var (property, column) in mappings)
                {
                    object? value = row[column];
                    Type? nullableType = Nullable.GetUnderlyingType(property.PropertyType);
                    Type valueType = nullableType ?? property.PropertyType;
                    if (value == DBNull.Value)
                    {
                        // 明确还原null，覆盖模型构造函数或属性初始值。
                        if (nullableType != null || !valueType.IsValueType) property.SetValue(item, null);
                        continue;
                    }
                    if (!valueType.IsInstanceOfType(value))
                    {
                        if (valueType.IsEnum) value = value is string text ? Enum.Parse(valueType, text) : Enum.ToObject(valueType, value);
                        else if (valueType == typeof(Guid)) value = Guid.Parse(value.ToString()!);
                        else value = Convert.ChangeType(value, valueType, CultureInfo.CurrentCulture);
                    }
                    property.SetValue(item, value);
                }
                result.Add(item);
            }
            return result;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>    
        /// <para>是否为NULL或者为空</para>    
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this DataTable dt)
        {
            return dt == null || dt.Columns.Count == 0 || dt.Rows.Count <= 0;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>根据条件筛选DataTable的行（类似Linq的Where）</para>
        /// </summary>
        /// <param name="dt">源DataTable</param>
        /// <param name="predicate">筛选条件</param>
        /// <returns>筛选后的新DataTable</returns>
        public static DataTable Where(this DataTable dt, Func<DataRow, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            // null源返回空表；有结构的空表保留结构。
            if (dt == null) return new DataTable();
            if (dt.Rows.Count == 0) return dt.Clone();

            // 筛选符合条件的行并创建新表
            var filteredRows = dt.Rows.Cast<DataRow>().Where(predicate).ToArray();
            var newDt = dt.Clone();
            foreach (var row in filteredRows)
            {
                newDt.ImportRow(row);
            }
            return newDt;
        }
    }
}
