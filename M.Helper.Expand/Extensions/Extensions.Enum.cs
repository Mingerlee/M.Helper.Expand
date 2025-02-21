using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace M.Helper.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// 获取枚举值的描述信息
        /// </summary>
        /// <param name="source">枚举值</param>
        /// <returns></returns>
        public static string GetDescription(this Enum source)
        {
            Type attributeType = typeof(DescriptionAttribute);
            FieldInfo[] fields = source.GetType().GetFields();
            string str = string.Empty;
            foreach (FieldInfo info in fields)
            {
                if (info.FieldType.IsEnum && info.Name.Equals(source.ToString()))
                {
                    object[] customAttributes = info.GetCustomAttributes(attributeType, true);
                    if (customAttributes.Length > 0)
                    {
                        DescriptionAttribute attribute = (DescriptionAttribute)customAttributes[0];
                        return attribute.Description;
                    }
                    return info.Name;
                }
            }
            return str;
        }

        /// <summary>
        /// 将枚举转换为数字
        /// </summary>
        /// <param name="source">枚举值</param>
        /// <returns></returns>
        public static int ToInt32(this Enum source)
        {
            return Convert.ToInt32(source);
        }
        /// <summary>
        /// 将枚举类转换成下拉列表
        /// </summary>
        /// <typeparam name="T">枚举类</typeparam>
        /// <param name="comboBox"></param>
        /// <param name="values">指定值集合</param>
        /// <returns></returns>
        public static ComboBoxIntViewModel ConvertComboBox<T>(this ComboBoxIntViewModel comboBox, IEnumerable<int> values = null) where T : Enum
        {
            var selectOptions = Enum.GetValues(typeof(T)).Cast<T>()
               .Select(x => new ComboBoxIntModel { Text = x.ToString(), Value = x.ToInt32() });

            if (values != null && values.Any())
            {
                selectOptions = selectOptions.Where(x => values.Contains(x.Value));
            }

            comboBox.SelectOptions= selectOptions;

            return comboBox;
        }
    }
}
