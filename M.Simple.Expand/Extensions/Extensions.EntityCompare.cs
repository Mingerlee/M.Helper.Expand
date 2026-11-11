using M.Simple.Expand.Attributes;
using M.Simple.Expand.Models;
using System.Reflection;

namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        ///// <summary>
        ///// <para>M.Simple.Expand扩展</para>
        /////<para> 比较两个实体值是否有修改过，返回修改结果集合</para>
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="t"></param>
        ///// <param name="compareModel">对比的model实体</param>
        ///// <param name="isAttr">是否有CompareValueAttribute特性标记</param>
        ///// <returns>修改结果集合</returns>
        //public static List<ComparesResults> CompareModel<T>(this T t, T compareModel, bool isAttr = false)
        //{
        //    List<ComparesResults> data = new List<ComparesResults>();
        //    if (t == null || compareModel == null)
        //    {
        //        return data;
        //    }
        //    PropertyInfo[] newProperties = t.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        //    PropertyInfo[] oldProperties = compareModel.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        //    if (newProperties.Length <= 0 || oldProperties.Length <= 0)
        //    {
        //        return data;
        //    }
        //    var oldFieldLst = new Dictionary<string, string>();
        //    foreach (PropertyInfo item in oldProperties)
        //    {
        //        if (isAttr)
        //        {
        //            bool hasFieldAttribute = item.CustomAttributes.Any(t => t.AttributeType == typeof(CompareValueAttribute));
        //            if (!hasFieldAttribute)
        //                continue;
        //        }
        //        string name = item.Name;//实体类字段名称
        //        string value = item.GetValue(compareModel, null)?.ToString() ?? "";//该字段的值
        //        if (item.PropertyType.IsValueType || item.PropertyType.Name.StartsWith("String"))
        //        {
        //            oldFieldLst.Add(name, value);//在此可转换value的类型
        //        }
        //    }
        //    foreach (PropertyInfo item in newProperties)
        //    {
        //        if (isAttr)
        //        {
        //            bool hasFieldAttribute = item.CustomAttributes.Any(t => t.AttributeType == typeof(CompareValueAttribute));
        //            if (!hasFieldAttribute)
        //                continue;
        //        }
        //        if (item.PropertyType.IsValueType || item.PropertyType.Name.StartsWith("String"))
        //        {
        //            string value = item.GetValue(t, null)?.ToString() ?? "";//该字段的值
        //            string filedName = item.Name;//实体类字段名称;
        //            if (oldFieldLst.ContainsKey(filedName))
        //            {
        //                string olddata = oldFieldLst[filedName];
        //                if (olddata != value)
        //                {
        //                    data.Add(new ComparesResults { FiledName = filedName, FiledOldValue = olddata, FiledNewValue = value, IsModify = true });
        //                }
        //            }
        //            else
        //            {
        //                data.Add(new ComparesResults { FiledName = filedName, FiledNewValue = value, IsAdded = true });
        //            }
        //        }
        //    }
        //    return data;
        //}
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para> 比较两个实体值是否有修改过，返回修改结果集合</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="newModel">新实体</param>
        /// <param name="oldModel">对比的旧实体</param>
        /// <param name="isAttr">是否只比较有CompareValueAttribute特性标记的属性</param>
        /// <returns>修改结果集合</returns>
        public static List<ComparesResults> CompareModel<T>(this T newModel, T oldModel, bool isAttr = false)
        {
            var results = new List<ComparesResults>();
            if (newModel == null && oldModel == null) return results;
            // 比较真实属性值，避免文本格式、类型过滤或空文本掩盖变化。
            Type type = typeof(T);
            foreach (var prop in GetProperties(type, isAttr))
            {
                object? oldValue = oldModel == null ? null : prop.GetValue(oldModel);
                object? newValue = newModel == null ? null : prop.GetValue(newModel);
                if (newModel != null && oldModel != null && Equals(oldValue, newValue)) continue;
                results.Add(new ComparesResults
                {
                    FiledName = prop.Name,
                    FiledOldValue = oldValue?.ToString(),
                    FiledNewValue = newValue?.ToString(),
                    IsAdded = oldModel == null,
                    IsDelete = newModel == null,
                    IsModify = newModel != null && oldModel != null
                });
            }
            return results;
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
            PropertyInfo[] newProperties = t.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetMethod?.IsPublic == true && p.SetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0).ToArray();
            PropertyInfo[] oldProperties = modifyModel.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0).ToArray();
            if (newProperties.Length <= 0 || oldProperties.Length <= 0)
            {
                return;
            }
            var oldFieldLst = new Dictionary<string, object?>();
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
                object? value = item.GetValue(modifyModel, null);//保留null，避免清空属性时被空串掩盖
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
                object? value = item.GetValue(t, null);//该字段的值

                if (oldFieldLst.ContainsKey(filedName))
                {
                    object? olddata = oldFieldLst[filedName];
                    if (!Equals(olddata, value))
                    {
                        item.SetStringValue(t, olddata?.ToString());
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
        public static void SetStringValue(this PropertyInfo property, object? obj, string? value)
        {
            ArgumentNullException.ThrowIfNull(property);
            if (property.SetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0)
                throw new ArgumentException("属性必须可公开写入且不能是索引器。", nameof(property));
            Type? underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
            Type valueType = underlyingType ?? property.PropertyType;
            // 可空值类型的空文本表示清空；字符串保持原值，不把空串变成null。
            if (valueType == typeof(string))
            {
                property.SetValue(obj, value);
                return;
            }
            if (value == null || (underlyingType != null && string.IsNullOrWhiteSpace(value)))
            {
                if (!property.PropertyType.IsValueType || underlyingType != null)
                {
                    property.SetValue(obj, null);
                    return;
                }
                throw new FormatException("非可空属性不能赋null。");
            }
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException("非可空属性不能赋空白文本。");
            // 按实际目标类型转换，避免byte/short收到装箱int，非法输入不再静默覆盖原值。
            var converter = System.ComponentModel.TypeDescriptor.GetConverter(valueType);
            if (!converter.CanConvertFrom(typeof(string)))
                throw new NotSupportedException($"不支持将字符串转换为{valueType.FullName}。");
            object? converted = converter.ConvertFromString(null, System.Globalization.CultureInfo.CurrentCulture, value);
            property.SetValue(obj, converted);
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
                result = ((IFormattable)value).ToString("0.0000", System.Globalization.CultureInfo.CurrentCulture);
            }
            else if (property.PropertyType == typeof(string))
            {
                result = value.ToString();
            }

            return result;
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isAttr"></param>
        /// <returns></returns>
        private static List<PropertyInfo> GetProperties(Type type, bool isAttr)
        {
            // object及其他引用类型属性也参与比较；引用对象沿用自身Equals，不递归遍历。
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0)
                .Where(p => !isAttr || p.IsDefined(typeof(CompareValueAttribute), inherit: false))
                .ToList();
        }
    }
    //internal static class PropertyCache
    //{
    //    // 缓存类型的属性信息（过滤后）
    //    // 核心缓存（线程安全）
    //    private static readonly ConcurrentDictionary<Type, Dictionary<bool, List<PropertyInfo>>> _propertyCache = new();
    //    // 记录缓存的最后访问时间（用于清理）
    //    private static readonly ConcurrentDictionary<Type, DateTime> _lastAccessTime = new();
    //    /// <summary>
    //    /// 获取指定类型的有效属性列表
    //    /// </summary>
    //    /// <param name="type">实体类型</param>
    //    /// <param name="isAttr">是否只包含CompareValueAttribute标记的属性</param>
    //    /// <returns>过滤后的属性列表</returns>
    //    public static List<PropertyInfo> GetProperties(Type type, bool isAttr)
    //    {
    //        // 更新最后访问时间
    //        _lastAccessTime[type] = DateTime.Now;

    //        return _propertyCache.GetOrAdd(type, t =>
    //        {
    //            var allProperties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
    //            var valueTypeProperties = allProperties.Where(p =>
    //                p.PropertyType.IsValueType || p.PropertyType == typeof(string)).ToList();

    //            var attrProperties = valueTypeProperties
    //                .Where(p => p.IsDefined(typeof(CompareValueAttribute), inherit: false))
    //                .ToList();

    //            var nonAttrProperties = valueTypeProperties.Except(attrProperties).ToList();

    //            return new Dictionary<bool, List<PropertyInfo>>
    //            {
    //                [true] = attrProperties,
    //                [false] = valueTypeProperties
    //            };
    //        })[isAttr];
    //    }

    //    /// <summary>
    //    /// 清理过期缓存（建议在后台线程定期调用）
    //    /// </summary>
    //    /// <param name="expireMinutes">缓存过期分钟数</param>
    //    public static void CleanExpiredCache(int expireMinutes = 30)
    //    {
    //        var expireTime = DateTime.Now.AddMinutes(-expireMinutes);
    //        var expiredTypes = _lastAccessTime.Where(kv => kv.Value < expireTime).Select(kv => kv.Key).ToList();

    //        foreach (var type in expiredTypes)
    //        {
    //            _propertyCache.TryRemove(type, out _);
    //            _lastAccessTime.TryRemove(type, out _);
    //        }
    //    }
    //}
}
