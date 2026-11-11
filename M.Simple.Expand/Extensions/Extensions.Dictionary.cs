namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>存在时更改,不存在时添加</para>
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="dictionary">之前字典</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>添加后的字典</returns>
        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) where TKey : notnull
        {
            dictionary ??= [];
            if (dictionary.TryAdd(key, value))
            {
                return dictionary;
            }
            else
            {
                dictionary[key] = value;
            }

            return dictionary;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>    
        /// <para>存在时更改,不存在时添加</para>    
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="targetDictionary">之前字典</param>
        /// <param name="sourceDictionary">Key-Value需要添加的字典</param>
        /// <returns>添加后的字典</returns>
        public static Dictionary<TKey, TValue> Merges<TKey, TValue>(this Dictionary<TKey, TValue> targetDictionary, Dictionary<TKey, TValue> sourceDictionary) where TKey : notnull
        {
            if (sourceDictionary == null)
            {
                return targetDictionary;
            }

            // 空目标只初始化一次，确保每次合并结果保留在返回字典中。
            targetDictionary ??= new Dictionary<TKey, TValue>(sourceDictionary.Comparer);
            foreach (var keyValuePair in sourceDictionary)
            {
                targetDictionary.Merge(keyValuePair.Key, keyValuePair.Value);
            }

            return targetDictionary;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>        
        /// <para>存在时忽略,不存在时添加</para>        
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="dictionary">之前字典</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>添加后的字典</returns>
        public static Dictionary<TKey, TValue> MergeIgnore<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) where TKey : notnull
        {
            dictionary ??= [];

            dictionary.TryAdd(key, value);

            return dictionary;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>            
        /// <para>存在时忽略,不存在时添加</para>
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="targetDictionary">之前字典</param>
        /// <param name="sourceDictionary">Key-Value需要添加的字典</param>
        /// <returns>添加后的字典</returns>
        public static Dictionary<TKey, TValue> MergeIgnores<TKey, TValue>(this Dictionary<TKey, TValue> targetDictionary, Dictionary<TKey, TValue> sourceDictionary) where TKey : notnull
        {
            if (sourceDictionary == null)
            {
                return targetDictionary;
            }

            // 空目标只初始化一次，确保每次合并结果保留在返回字典中。
            targetDictionary ??= new Dictionary<TKey, TValue>(sourceDictionary.Comparer);
            foreach (var keyValuePair in sourceDictionary)
            {
                targetDictionary.MergeIgnore(keyValuePair.Key, keyValuePair.Value);
            }

            return targetDictionary;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>            
        /// <para>获取Value值</para>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key) where TKey : notnull
        {
            if (dic != null && dic.TryGetValue(key, out TValue? value))
            {
                return value;
            }
            return default;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>         
        /// <para>删除</para>
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        public static void Remove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull
        {
            if (dictionary != null && dictionary.ContainsKey(key))
            {
                dictionary.Remove(key);
            }
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>     
        /// <para>获取值之后，再删除</para>
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">Key</param>
        /// <returns>删除的值</returns>
        public static TValue RemovePop<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull
        {
            if (dictionary != null && dictionary.TryGetValue(key, out TValue? value))
            {
                TValue item = value;
                dictionary.Remove(key);
                return item;
            }

            return default;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>批量删除</para>
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="keys">Key集合</param>
        /// <returns>Value</returns>
        public static void Removes<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys) where TKey : notnull
        {
            foreach (var key in keys)
            {
                dictionary.Remove(key);
            }
        }
    }
}
