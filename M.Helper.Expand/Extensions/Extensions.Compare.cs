using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace M.Helper.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// 比较两个实体值是否有修改过，返回修改结果集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareModel">对比的model实体</param>
        /// <param name="isAttr">是否有CompareValueAttribute特性标记</param>
        /// <returns></returns>
        public static List<Results> CompareModel<T>(this T t, T compareModel, bool isAttr = false)
        {
            List<Results> data = new List<Results>();
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
                string value = item.GetValue(t, null)?.ToString() ?? "";//该字段的值
                string filedName = item.Name;//实体类字段名称;
                if (oldFieldLst.ContainsKey(filedName))
                {
                    string olddata = oldFieldLst[filedName];
                    if (olddata != value)
                    {
                        data.Add(new Results { FiledName = filedName, FiledOldValue = olddata, FiledNewValue = value, IsModify = true });
                    }
                }
                else
                {
                    data.Add(new Results { FiledName = filedName, FiledNewValue = value, IsAdded = true });
                }

            }
            return data;
        }
        /// <summary>
        /// 比较两个实体值是否有修改过，返回bool值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareModel">对比的model实体</param>
        /// <param name="isAttr">是否有CompareValueAttribute特性标记</param>
        /// <returns></returns>
        public static bool IsModify<T>(this T t, T compareModel, bool isAttr = false)
        {
            return CompareModel(t, compareModel, isAttr).Count > 0;
        }
        /// <summary>
        ///     修改实体指定字段
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="t"></param>
        /// <param name="modifyModel"></param>
        /// <param name="isAttr"></param>
        public static void ModifyField<T,T2>(this T t, T2 modifyModel, bool isAttr = false)
        {
            if (t == null || modifyModel == null)
            {
                return ;
            }
            PropertyInfo[] newProperties = t.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo[] oldProperties = modifyModel.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (newProperties.Length <= 0 || oldProperties.Length <= 0)
            {
                return ;
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
                name =string.IsNullOrEmpty(name)?item.Name: name;
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

                string filedName =string.Empty ;//实体类字段名称;

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
                        item.SetValue(t, olddata, null);
                    }
                }
            }
        }
    }
}
