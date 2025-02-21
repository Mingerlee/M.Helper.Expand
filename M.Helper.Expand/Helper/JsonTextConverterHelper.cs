using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace M.Helper.Expand
{
    /// <summary>
    ///     
    /// </summary>
    public class JsonTextConverterHelper
    {
    }
    /// <summary>
    ///     System.Text.Json 
    ///     四舍五入，保留两位小数
    /// </summary>
    public class SysTextDecimalConverter : JsonConverter<decimal>
    {
        /// <summary>
        ///     
        /// </summary>
        /// <param name="typeToConvert"></param>
        /// <returns></returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(decimal);
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Convert.ToDecimal(reader.GetString());
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(Math.Round(value, 2));
        }
    }
    /// <summary>
    ///     日期格式化：yyyy/MM/dd
    /// </summary>
    public class SysTextDateFormatConverter : JsonConverter<DateTime?>
    {
        /// <summary>
        ///     
        /// </summary>
        /// <param name="typeToConvert"></param>
        /// <returns></returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(DateTime);
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                return DateTime.Parse(reader.GetString());
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteStringValue("");
            }
            else
            {
                writer.WriteStringValue(value.Value.ToString("yyyy/MM/dd"));
            }
        }
    }
}
