using Microsoft.International.Converters.PinYinConverter;
using System.Text;

namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>隐藏部分字符</para>
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string ToCover(this string s, char c = '*')
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (string.IsNullOrEmpty(c.ToString())) return s;

            switch (s.Length)
            {
                case 1: return s;
                case 2: return s.Substring(0, 1) + c;
                default:
                    if (s.Length > 10) return s.Substring(0, 3).PadRight(s.Length - 4, c) + s.Substring(s.Length - 4, 4);
                    else if (s.Length > 4) return s.Substring(0, 2).PadRight(s.Length - 2, c) + s.Substring(s.Length - 2, 2);
                    else if (s.Length == 4) return s.Substring(0, 1).PadRight(s.Length - 1, c) + s.Substring(s.Length - 1, 1);
                    else return s.Substring(0, 1) + c + s.Substring(s.Length - 1, 1);
            }
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>汉字转化为拼音首字母</para>
        /// </summary>
        /// <param name="chineseStr">汉字</param>
        /// <returns>首字母</returns>
        public static string ToFirstPinyin(this string chineseStr)
        {
            string r = string.Empty;
            foreach (char obj in chineseStr)
            {
                try
                {
                    ChineseChar chineseChar = new(obj);
                    string t = chineseChar.Pinyins[0].ToString();
                    r += t.Substring(0, 1);
                }
                catch
                {
                    r += obj.ToString();
                }
            }
            return r;
        }
        /// <summary>
        ///<para>M.Simple.Expand扩展</para>
        /// <para>汉字转化为拼音</para>
        /// </summary>
        /// <param name="chineseStr">汉字</param>
        /// <returns>全拼</returns>
        public static string ToPinYin(this string chineseStr)
        {
            string r = string.Empty;
            if (string.IsNullOrWhiteSpace(chineseStr))
            {
                return r;
            }
            foreach (char obj in chineseStr)
            {
                try
                {
                    ChineseChar chineseChar = new(obj);
                    string t = chineseChar.Pinyins[0].ToString();
                    r += t.Substring(0, t.Length - 1);
                }
                catch
                {
                    r += obj.ToString();
                }
            }
            return r;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>自身值为null或者空字符，则取默认值</para>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetValueOrDefault(this string value, string defaultValue="")
        {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>获取字节长度</para>
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int GetRealLength(this string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return 0;
            }
            return Encoding.Default.GetByteCount(source);
        }
    }
}
