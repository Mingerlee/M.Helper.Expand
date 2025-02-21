using Newtonsoft.Json;

namespace M.Helper.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="formatDate"></param>
        /// <returns></returns>
        public static string Serialize(this object obj, JsonSerializerSettings? formatDate = null)
        {
            if (obj == null) return string.Empty;
            formatDate = formatDate ?? new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-dd HH:mm:ss"
            };

            return JsonConvert.SerializeObject(obj, formatDate);
        }
        /// <summary>
        ///     反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityString"></param>
        /// <returns></returns>
        public static T DeserializeObject<T>(this string entityString)
        {
            if (string.IsNullOrEmpty(entityString))
            {
                return default(T);
            }
            if (entityString == "{}")
            {
                entityString = "[]";
            }
            return JsonConvert.DeserializeObject<T>(entityString);
        }
    }
}
