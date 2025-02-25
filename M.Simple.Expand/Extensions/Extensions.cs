using M.Simple.Expand.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand
{
    /// <summary>
    ///     M.Simple.Expand扩展
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>获取雪花编码</para>
        /// </summary>
        /// <returns></returns>
        public static string GetSnowCode()
        {
            return KeyCodeHelper.GetSnowCode();
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>获取雪花编码集合</para>
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetSnowCodes(int number)
        {
            List<string> results = [];
            for (int i = 0; i < number; i++)
            {
                results.Add(KeyCodeHelper.GetSnowCode());
            }
            return results;
        }
    }
}
