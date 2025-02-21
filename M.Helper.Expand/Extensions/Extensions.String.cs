using Microsoft.International.Converters.PinYinConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace M.Helper.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// 隐藏部分字符
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
        /// 汉字转化为拼音首字母
        /// </summary>
        /// <param name="chineseStr">汉字</param>
        /// <returns>首字母</returns>
        public static string GetFirstPinyin(this string chineseStr)
        {
            string r = string.Empty;
            foreach (char obj in chineseStr)
            {
                try
                {
                    ChineseChar chineseChar = new ChineseChar(obj);
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
        ///     汉字转化为拼音
        /// </summary>
        /// <param name="chineseStr">汉字</param>
        /// <returns>全拼</returns>
        public static string ConvertToPinYin(this string chineseStr)
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
                    ChineseChar chineseChar = new ChineseChar(obj);
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
        ///     自身值为null或者空字符，则取默认值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetValueOrDefault(this string value,string defaultValue)
        {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
        /// <summary>
        /// 字节长度
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
        /// <summary>
        ///是否为int
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsInteger(this string source)
        {
            int num;
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }
            return int.TryParse(source, out num);
        }
        /// <summary>
        /// 是否为空或者NULL
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsNullOrEmptyString(this string source)
        {
            return string.IsNullOrEmpty(source);
        }
        /// <summary>
        /// 移除打印的字符
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string RemoveNonPrintChars(this string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }
            Regex regex = new Regex("[\0-\b\v\f\x000e-\x001f]");
            return regex.Replace(source, "");
        }
        /// <summary>
        /// 移除无效的字符
        /// </summary>
        /// <param name="filenameNoDir"></param>
        /// <param name="replaceWith"></param>
        /// <returns></returns>
        public static string ReplaceNonValidChars(this string filenameNoDir, string replaceWith)
        {
            if (string.IsNullOrEmpty(filenameNoDir))
            {
                return string.Empty;
            }
            return Regex.Replace(filenameNoDir, "[\\<\\>\\/\\\\\\|\\:\"\\*\\?\\r\\n]", replaceWith, RegexOptions.Compiled);
        }
        /// <summary>
        /// 是否为布尔值
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool ToBoolean(this string source)
        {
            bool flag;
            return ((!string.IsNullOrEmpty(source) && bool.TryParse(source, out flag)) && flag);
        }
        /// <summary>
        /// 转换为默认日期-时间
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string source)
        {
            return source.ToDateTime(DateTime.Now);
        }
        /// <summary>
        /// 转换为默认日期-时间
        /// </summary>
        /// <param name="source"></param>
        /// <param name="defaultValue">默认日期-时间</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string source, DateTime defaultValue)
        {
            DateTime time;
            if (!string.IsNullOrEmpty(source) && DateTime.TryParse(source, out time))
            {
                return time;
            }
            return defaultValue;
        }
        /// <summary>
        ///     转换为默认日期-时间
        /// </summary>
        /// <param name="source"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static DateTime? ToDateTime(this string source, DateTime? defaultValue)
        {
            DateTime time;
            if (!string.IsNullOrEmpty(source) && DateTime.TryParse(source, out time))
            {
                return time;
            }
            return defaultValue;
        }
        public static double ToDouble(this string source)
        {
            return source.ToDouble(-1.0);
        }

        public static double ToDouble(this string source, double defaultValue)
        {
            double num;
            if (!string.IsNullOrEmpty(source) && double.TryParse(source, out num))
            {
                return num;
            }
            return defaultValue;
        }
        /// <summary>
        /// 转换为枚举值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T ToEnum<T>(this string source, T defaultValue)
        {
            if (!string.IsNullOrEmpty(source))
            {
                try
                {
                    T local = (T)Enum.Parse(typeof(T), source, true);
                    if (Enum.IsDefined(typeof(T), local))
                    {
                        return local;
                    }
                }
                catch
                {
                }
            }
            return defaultValue;
        }

        public static T ToEnumExt<T>(this string source, T defaultValue)
        {
            if (!string.IsNullOrEmpty(source))
            {
                try
                {
                    return (T)Enum.Parse(typeof(T), source, true);
                }
                catch
                {
                }
            }
            return defaultValue;
        }

        public static int ToInt32(this string source)
        {
            return source.ToInt32(-1);
        }
        public static decimal ToDecimal(this string source, decimal defaultValue)
        {
            decimal num;
            if (!string.IsNullOrEmpty(source) && decimal.TryParse(source, out num))
            {
                return num;
            }
            return defaultValue;
        }

        public static int ToInt32(this string source, int defaultValue)
        {
            int num;
            if (!string.IsNullOrEmpty(source) && int.TryParse(source, out num))
            {
                return num;
            }
            return defaultValue;
        }

        public static long ToInt64(this string source)
        {
            return source.ToInt64(-1L);
        }

        public static long ToInt64(this string source, long defaultValue)
        {
            long num;
            if (!string.IsNullOrEmpty(source) && long.TryParse(source, out num))
            {
                return num;
            }
            return defaultValue;
        }

        public static string ToUnicode(this string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < source.Length; i++)
            {
                byte[] bytes = Encoding.Unicode.GetBytes(source.Substring(i, 1));
                if (bytes.Length > 1)
                {
                    string str = Convert.ToString((short)bytes[1], 0x10);
                    string str2 = Convert.ToString((short)bytes[0], 0x10);
                    str = ((str.Length == 1) ? "0" : "") + str;
                    str2 = ((str2.Length == 1) ? "0" : "") + str2;
                    builder.Append("&#" + Convert.ToInt32(str + str2, 0x10) + ";");
                }
            }
            return builder.ToString();
        }

        public static string ToUTF8(this string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < source.Length; i++)
            {
                byte[] bytes = Encoding.Unicode.GetBytes(source.Substring(i, 1));
                if (bytes.Length > 1)
                {
                    string str = Convert.ToString((short)bytes[1], 0x10);
                    string str2 = Convert.ToString((short)bytes[0], 0x10);
                    builder.Append(@"\u" + (((str.Length == 1) ? "0" : "") + str) + (((str2.Length == 1) ? "0" : "") + str2));
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Converts given PascalCase/camelCase string to sentence (by splitting words by space).
        /// Example: "ThisIsSampleSentence" is converted to "This is a sample sentence".
        /// </summary>
        /// <param name="str">String to convert.</param>
        /// <param name="culture">An object that supplies culture-specific casing rules.</param>
        public static string ToSentenceCase(this string str, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            return Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1], culture));
        }

        /// <summary>
        /// 字符串数组中添加字符
        /// </summary>
        /// <param name="source"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string JoinAsString(this IEnumerable<string> source, string separator)
        {
            return string.Join(separator, source);
        }

        /// <summary>
        /// 字符串去除html格式
        /// </summary>
        /// <param name="Htmlstring"></param>
        /// <returns></returns>
        public static string ReplaceHTMLChar(this string Htmlstring)  //替换HTML标记
        {
            if (string.IsNullOrEmpty(Htmlstring))
            {
                return string.Empty;
            }
            //删除脚本
            Htmlstring = Regex.Replace(Htmlstring, @"<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase);

            //删除HTML
            Htmlstring = Regex.Replace(Htmlstring, @"<(.[^>]*)>", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"-->", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"<!--.*", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(quot|#34);", "\"", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(amp|#38);", "&", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(lt|#60);", "<", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(gt|#62);", ">", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(nbsp|#160);", " ", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(iexcl|#161);", "\xa1", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(cent|#162);", "\xa2", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(pound|#163);", "\xa3", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(copy|#169);", "\xa9", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&#(\d+);", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"<img[^>]*>;", "", RegexOptions.IgnoreCase);
            Htmlstring.Replace("<", "");
            Htmlstring.Replace(">", "");
            Htmlstring.Replace("\r\n", "");
            Htmlstring = System.Web.HttpUtility.HtmlEncode(Htmlstring).Trim();
            return Htmlstring;
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="source"></param>
        /// <param name="resultLength"></param>
        /// <returns></returns>
        public static string SubString(this string source, int resultLength)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }
            if (Encoding.Default.GetByteCount(source) > resultLength)
            {
                int num = 0;
                int length = 0;
                foreach (char ch in source)
                {
                    if (ch > '\x007f')
                    {
                        num += 2;
                    }
                    else
                    {
                        num++;
                    }
                    if (num > resultLength)
                    {
                        source = source.Substring(0, length);
                        return source;
                    }
                    length++;
                }
            }
            return source;
        }

    }
}
