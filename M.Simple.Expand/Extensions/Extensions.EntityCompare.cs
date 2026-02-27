using M.Simple.Expand.Attributes;
using M.Simple.Expand.Models;
using System.Reflection;

namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        ///<para> 比较两个实体值是否有修改过，返回修改结果集合</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareModel">对比的model实体</param>
        /// <param name="isAttr">是否有CompareValueAttribute特性标记</param>
        /// <returns>修改结果集合</returns>
        public static List<ComparesResults> CompareModel<T>(this T t, T compareModel, bool isAttr = false)
        {
            List<ComparesResults> data = new List<ComparesResults>();
            if (t == null || compareModel == null)
            {
                return data;
            }
            PropertyInfo[] newProperties = t.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo[] oldProperties = compareModel.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (newProperties.Length <= 0 || oldProperties.Length <= 0)
            {
                return data;
            }
            var oldFieldLst = new Dictionary<string, string>();
            foreach (PropertyInfo item in oldProperties)
            {
                if (isAttr)
                {
                    bool hasFieldAttribute = item.CustomAttributes.Any(t => t.AttributeType == typeof(CompareValueAttribute));
                    if (!hasFieldAttribute)
                        continue;
                }
                string name = item.Name;//实体类字段名称
                string value = item.GetValue(compareModel, null)?.ToString() ?? "";//该字段的值
                if (item.PropertyType.IsValueType || item.PropertyType.Name.StartsWith("String"))
                {
                    oldFieldLst.Add(name, value);//在此可转换value的类型
                }
            }
            foreach (PropertyInfo item in newProperties)
            {
                if (isAttr)
                {
                    bool hasFieldAttribute = item.CustomAttributes.Any(t => t.AttributeType == typeof(CompareValueAttribute));
                    if (!hasFieldAttribute)
                        continue;
                }
                if (item.PropertyType.IsValueType || item.PropertyType.Name.StartsWith("String"))
                {
                    string value = item.GetValue(t, null)?.ToString() ?? "";//该字段的值
                    string filedName = item.Name;//实体类字段名称;
                    if (oldFieldLst.ContainsKey(filedName))
                    {
                        string olddata = oldFieldLst[filedName];
                        if (olddata != value)
                        {
                            data.Add(new ComparesResults { FiledName = filedName, FiledOldValue = olddata, FiledNewValue = value, IsModify = true });
                        }
                    }
                    else
                    {
                        data.Add(new ComparesResults { FiledName = filedName, FiledNewValue = value, IsAdded = true });
                    }
                }
            }
            return data;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>比较两个实体值是否有修改过</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareModel">对比的model实体</param>
        /// <param name="isAttr">是否有CompareValueAttribute特性标记</param>
        /// <returns>有修改返回true，否则返回false</returns>
        public static bool IsModify<T>(this T t, T compareModel, bool isAttr = false)
        {
            return CompareModel(t, compareModel, isAttr).Count > 0;
        }
        /// <summary>
        ///<para>M.Simple.Expand扩展</para>
        /// <para>修改实体指定字段</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="t"></param>
        /// <param name="modifyModel"></param>
        /// <param name="isAttr"></param>
        public static void ModifyField<T, T2>(this T t, T2 modifyModel, bool isAttr = false)
        {
            if (t == null || modifyModel == null)
            {
                return;
            }
            PropertyInfo[] newProperties = t.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo[] oldProperties = modifyModel.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (newProperties.Length <= 0 || oldProperties.Length <= 0)
            {
                return;
            }
            var oldFieldLst = new Dictionary<string, string>();
            foreach (PropertyInfo item in oldProperties)
            {
                if (isAttr)
                {
                    bool hasFieldAttribute = item.CustomAttributes.Any(t => t.AttributeType == typeof(ModifyValueAttribute));
                    if (!hasFieldAttribute)
                        continue;
                }
                string name = string.Empty;//实体类字段名称

                var customAttribute = item.GetCustomAttribute(typeof(ModifyValueAttribute));
                if (customAttribute != null)
                {
                    ModifyValueAttribute att = (ModifyValueAttribute)customAttribute;
                    if (att != null)
                    {
                        name = att.FlidName;
                    }
                }
                name = string.IsNullOrEmpty(name) ? item.Name : name;
                string value = item.GetValue(modifyModel, null)?.ToString() ?? "";//该字段的值
                if (item.PropertyType.IsValueType || item.PropertyType.Name.StartsWith("String"))
                {
                    oldFieldLst.Add(name, value);//在此可转换value的类型
                }
            }
            foreach (PropertyInfo item in newProperties)
            {
                if (isAttr)
                {
                    bool hasFieldAttribute = item.CustomAttributes.Any(t => t.AttributeType == typeof(ModifyValueAttribute));
                    if (!hasFieldAttribute)
                        continue;
                }

                string filedName = string.Empty;//实体类字段名称;

                var customAttribute = item.GetCustomAttribute(typeof(ModifyValueAttribute));
                if (customAttribute != null)
                {
                    ModifyValueAttribute att = (ModifyValueAttribute)customAttribute;
                    if (att != null)
                    {
                        filedName = att.FlidName;
                    }
                }
                filedName = string.IsNullOrEmpty(filedName) ? item.Name : filedName;
                string value = item.GetValue(t, null)?.ToString() ?? "";//该字段的值

                if (oldFieldLst.ContainsKey(filedName))
                {
                    string olddata = oldFieldLst[filedName];
                    if (olddata != value)
                    {
                        item.SetStringValue(t, olddata);
                    }
                }
            }
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>设置属性的值</para>
        /// </summary>
        /// <param name="property"></param>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetStringValue(this PropertyInfo property,object? obj,string value)
        {
            switch (property.PropertyType.FullName)
            {
                case "System.String":
                    property.SetValue(obj, value, null);
                    break;
                case "System.DateTime":
                    if (DateTime.TryParse(value, out DateTime dt))
                        property.SetValue(obj, dt, null);
                    else
                        property.SetValue(obj, DateTime.Now, null);
                    break;
                case "System.Boolean": //布尔型
                    if (bool.TryParse(value, out bool b))
                        property.SetValue(obj, b, null);
                    else
                        property.SetValue(obj, false, null);
                    break;
                case "System.Int16": //整型
                case "System.Int32":
                case "System.Byte":
                    if (int.TryParse(value, out int i))
                        property.SetValue(obj, i, null);
                    else
                        property.SetValue(obj, 0, null);
                    break;
                case "System.Int64":
                    if (long.TryParse(value, out long l))
                        property.SetValue(obj, l, null);
                    else
                        property.SetValue(obj, 0, null);
                    break;
                case "System.Decimal": //浮点型
                    if (decimal.TryParse(value, out decimal d))
                        property.SetValue(obj, d, null);
                    else
                        property.SetValue(obj, 0, null);
                    break;
                case "System.Single":
                    if (float.TryParse(value, out float f))
                        property.SetValue(obj, f, null);
                    else
                        property.SetValue(obj, 0, null);
                    break;
                case "System.Double":
                    if (double.TryParse(value, out double du))
                        property.SetValue(obj, du, null);
                    else
                        property.SetValue(obj, 0, null);
                    break;
                case "System.DBNull": //空值处理
                    property.SetValue(obj, null, null);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        ///    <para>M.Simple.Expand扩展</para> 
        ///    <para>获取属性的值</para>
        /// </summary>
        /// <param name="property"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetStringValue(this PropertyInfo property, object obj)
        {
            string result = string.Empty;
            var value = property.GetValue(obj, null);
            if (value == null) return result;
            if (string.IsNullOrWhiteSpace(value.ToString())) return result;

            if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
            {
                  result = ((DateTime)value).ToString("yyyy/MM/dd");
            }
            else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                result = ((bool)value).ToString();
            }
            else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
            {
                result = ((int)value).ToString();
            }
            else if (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?) || property.PropertyType == typeof(float) || property.PropertyType == typeof(float?) || property.PropertyType == typeof(double) || property.PropertyType == typeof(double?))
            {
                result = ((decimal)value).ToString("0.0000");
            }
            else if (property.PropertyType == typeof(string))
            {
                result = value.ToString();
            }

            return result;
        }
    }
}
