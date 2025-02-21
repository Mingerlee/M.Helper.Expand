using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Helper.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        ///     获取字典Value值
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static TValue GetDictionaryValue<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key)
        {
            if (dic != null && dic.ContainsKey(key))
            {
                return dic[key];
            }
            return default;
        }
        /// <summary>
        /// Put 扩展字典方法 存在时更改,不存在时添加
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="dictionary">Put之前字典</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>Put之后字典</returns>
        public static Dictionary<TKey, TValue> Put<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            return dictionary.Put(key, value, (v) => true);
        }

        /// <summary>
        /// Put 扩展字典方法 存在时更改,不存在时添加
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="targetDictionary">Put之前字典</param>
        /// <param name="sourceDictionary">Key-Value</param>
        /// <returns>Put之后字典</returns>
        public static Dictionary<TKey, TValue> PutArray<TKey, TValue>(this Dictionary<TKey, TValue> targetDictionary, Dictionary<TKey, TValue> sourceDictionary)
        {
            return targetDictionary.PutArray(sourceDictionary, (v) => true);
        }

        /// <summary>
        /// Put 扩展字典方法 存在时更改,不存在时添加
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="dictionary">Put之前字典</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="ignoreExists">是否忽视已存在项（不更改已存在项）</param>
        /// <returns>Put之后字典</returns>
        public static Dictionary<TKey, TValue> Put<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value, bool ignoreExists)
        {
            return dictionary.Put(key, value, (v) => !ignoreExists);
        }

        /// <summary>
        /// Put 扩展字典方法 存在时更改,不存在时添加
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="dictionary">Put之前字典</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="replaceExistsValueCondition">替换已存在项的条件函数</param>
        /// <returns>Put之后字典</returns>
        public static Dictionary<TKey, TValue> Put<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value, Func<TValue, bool> replaceExistsValueCondition)
        {
            if (dictionary != null)
            {
                if (dictionary.ContainsKey(key) && replaceExistsValueCondition(value))
                {
                    dictionary[key] = value;
                }
                else
                {
                    dictionary.Add(key, value);
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Put 扩展字典方法 存在时更改,不存在时添加
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="targetDictionary">Put之前字典</param>
        /// <param name="sourceDictionary">Key-Value</param>
        /// <param name="ignoreExists">是否忽视已存在项（不更改已存在项）</param>
        /// <returns>Put之后字典</returns>
        public static Dictionary<TKey, TValue> PutArray<TKey, TValue>(this Dictionary<TKey, TValue> targetDictionary, Dictionary<TKey, TValue> sourceDictionary, bool ignoreExists)
        {
            return targetDictionary.PutArray(sourceDictionary, (v) => !ignoreExists);
        }


        /// <summary>
        /// Put 扩展字典方法 存在时更改,不存在时添加
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="targetDictionary">Put之前字典</param>
        /// <param name="sourceDictionary">Key-Value</param>
        /// <param name="replaceExistsValueCondition">替换已存在项的条件函数</param>
        /// <returns>Put之后字典</returns>
        public static Dictionary<TKey, TValue> PutArray<TKey, TValue>(this Dictionary<TKey, TValue> targetDictionary, Dictionary<TKey, TValue> sourceDictionary, Func<TValue, bool> replaceExistsValueCondition)
        {
            if (sourceDictionary == null)
            {
                return targetDictionary;
            }

            foreach (var keyValuePair in sourceDictionary)
            {
                targetDictionary.Put(keyValuePair.Key, keyValuePair.Value, replaceExistsValueCondition);
            }

            return targetDictionary;
        }

        /// <summary>
        /// Put 扩展字典方法 存在时更改,不存在时添加
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="dictionary">Put之前字典</param>
        /// <param name="keyValuePair">Key-Value</param>
        /// <returns>Put之后字典</returns>
        public static Dictionary<TKey, TValue> Put<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, KeyValuePair<TKey, TValue> keyValuePair)
        {
            return dictionary.Put(keyValuePair.Key, keyValuePair.Value);
        }

        /// <summary>
        /// 获取字典中的值
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        public static TValue GetObjectWithoutException<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TValue : class
        {
            if (dictionary != null && dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }

            return null;
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        public static void RemoveObjectWithoutException<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TValue : class
        {
            if (dictionary != null && dictionary.ContainsKey(key))
            {
                dictionary.Remove(key);
            }
        }

        /// <summary>
        /// 获取之后删除(类似队列的Pop) 
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        public static TValue PopWithoutException<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TValue : class
        {
            if (dictionary != null && dictionary.ContainsKey(key))
            {
                TValue item = dictionary[key];
                dictionary.Remove(key);
                return item;
            }

            return null;
        }

        /// <summary>
        /// 取值 
        /// </summary>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        public static TValue GetStructWithoutException<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TValue : struct
        {
            if (dictionary != null && dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }

            return default(TValue);
        }

        /// <summary>
        /// 列表转换成字典(重复数据自动忽略)
        /// </summary>
        /// <typeparam name="TModel">Model的类型</typeparam>
        /// <typeparam name="TKey">Key的类型</typeparam>
        /// <typeparam name="TValue">Value的类型</typeparam>
        /// <param name="list">列表</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数</param>
        /// <param name="elementSelector">用于从每个元素产生结果元素值的转换函数</param>
        /// <returns>Value</returns>
        public static Dictionary<TKey, TValue> ToDictionaryWithoutException<TModel, TKey, TValue>(this IEnumerable<TModel> list, Func<TModel, TKey> keySelector, Func<TModel, TValue> elementSelector)
        {
            var dict = new Dictionary<TKey, TValue>();
            if (list != null)
            {
                foreach (var model in list)
                {
                    var key = keySelector(model);
                    var value = elementSelector(model);
                    dict.Put(key, value);
                }
            }

            return dict;
        }
    }
}
