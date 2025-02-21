using Newtonsoft.Json;
using System;
namespace M.Helper.Expand
{
    /// <summary>
    ///     
    /// </summary>
    public class JsonConverterHelper
    {
    }
    /// <summary>
    ///     四舍五入 保留两位小数
    /// </summary>
    public class JsonDecimalConverter : JsonConverter
    {
        /// <summary>
        ///     
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(decimal);
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return existingValue;
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            decimal? dec = value as decimal?;
            if (dec == null) return;
            writer.WriteValue(Math.Round(dec.Value, 2));
        }
    }
    /// <summary>
    ///     日期格式化：yyyy/MM/dd
    /// </summary>
    public class JsonDateFormatConverter : JsonConverter
    {
        /// <summary>
        ///     
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime);
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return existingValue;
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            DateTime? dec = value as DateTime?;
            if (dec == null)
            {
                writer.WriteValue("");
            }
            else
            {
                writer.WriteValue(dec.Value.ToString("yyyy/MM/dd"));
            }
        }
    }
}
