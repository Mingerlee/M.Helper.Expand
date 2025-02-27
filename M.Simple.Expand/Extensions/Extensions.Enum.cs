using M.Simple.Expand.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>获取枚举值的描述信息</para>
        /// </summary>
        /// <param name="source">枚举值</param>
        /// <returns></returns>
        public static string GetDescription(this Enum source)
        {
            FieldInfo fieldInfo = source.GetType().GetField(source.ToString());

            DescriptionAttribute[] attrs = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            return attrs.Length > 0 ? attrs[0].Description : source.ToString();
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>将枚举值转换为Int32</para>
        /// </summary>
        /// <param name="source">枚举值</param>
        /// <returns></returns>
        public static int ToInt32(this Enum source)
        {
            return Convert.ToInt32(source);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>将枚举类转换成下拉列表</para>
        /// </summary>
        /// <typeparam name="T">枚举类</typeparam>
        /// <param name="comboBox"></param>
        /// <param name="values">指定值集合</param>
        /// <returns></returns>
        public static ComboBoxIntViewModel ToComboBoxs<T>(this ComboBoxIntViewModel comboBox, IEnumerable<int> values = null) where T : Enum
        {
            var selectOptions = Enum.GetValues(typeof(T)).Cast<T>()
               .Select(x => new ComboBoxIntModel { Text = x.ToString(), Value = x.ToInt32() });

            if (values != null && values.Any())
            {
                selectOptions = selectOptions.Where(x => values.Contains(x.Value));
            }

            comboBox.SelectOptions = selectOptions;

            return comboBox;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>直接获取特性</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static T GetAttributeOfType<T>(this Enum enumValue) where T : Attribute
        {
            Type type = enumValue.GetType();
            MemberInfo[] memInfo = type.GetMember(enumValue.ToString());
            object[] attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
            return (attributes.Length > 0) ? (T)attributes[0] : null;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        ///<para> 获取特性 (DisplayAttribute) 的名称(Name)</para>
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns>如果未使用该特性，则返回枚举的名称</returns>
        public static string GetDisplayName(this Enum enumValue)
        {
            FieldInfo fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
            DisplayAttribute[] attrs =  fieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];

            return attrs.Length > 0 ? attrs[0].Name : enumValue.ToString();
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>获取特性 (DisplayAttribute) 的说明(Description)</para>
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns>如果未使用该特性，则返回枚举的名称</returns>
        public static string GetDisplayDescription(this Enum enumValue)
        {
            FieldInfo fieldInfo = enumValue.GetType().GetField(enumValue.ToString());

            DisplayAttribute[] attrs =fieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];

            return attrs.Length > 0 ? attrs[0].Description : enumValue.ToString();
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>根据字符串转成指定枚举值</para>
        /// </summary>
        /// <returns>返回指定枚举值</returns>
        public static T ToEnum<T>(this string enumString) where T : Enum
        {
            try
            {
                return (T)Enum.Parse(typeof(T), enumString);
            }
            catch
            {
                return default;
            }
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>根据int转成指定枚举Value值</para>
        /// </summary>
        /// <returns>返回指定枚举Text值</returns>
        public static string ToEnumText<T>(this int enumValue) where T : Enum
        {
            try
            {
                var result= (T)Enum.ToObject(typeof(T), enumValue);
                return result.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>根据int转成指定枚举Value值</para>
        /// </summary>
        /// <returns>返回指定枚举Text值</returns>
        public static string ToEnumText<T>(this int? enumValue) where T : Enum
        {
            try
            {
                if (enumValue.HasValue)
                {
                    var result = (T)Enum.ToObject(typeof(T), enumValue);
                    return result.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>转成枚举值(Value)及描述值的字典</para>
        /// </summary>
        /// <returns>枚举值(Value)及描述值的字典</returns>
        public static IDictionary<int, string> ToValueDescriptions(this Type enumType)
        {
            if (enumType.IsEnum)
            {
                var dictionary = new Dictionary<int, string>();
                foreach (Enum code in Enum.GetValues(enumType))
                {
                    dictionary.Add(code.ToInt32(), code.GetDescription());
                }
                return dictionary;
            }
            return new Dictionary<int, string>();
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        ///<para> 转成枚举字符串值(Text)及描述值的字典</para>
        ///</summary>
        ///<returns>枚举字符串值(Text)及描述值的字典</returns>
        public static IDictionary<string, string> ToTextDescriptions(this Type enumType)
        {
            if (enumType.IsEnum)
            {
                var dictionary = new Dictionary<string, string>();
                foreach (Enum code in Enum.GetValues(enumType))
                {
                    dictionary.Add(code.ToString(), code.GetDescription());
                }
                return dictionary;
            }
            return new Dictionary<string, string>(); ;
        }
    }
}
