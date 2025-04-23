using Microsoft.International.Converters.PinYinConverter;
using System.Text;

namespace M.Simple.Expand
{
    /// <summary>
    ///     M.Simple.Expand扩展
    /// </summary>
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
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>全角转半角的函数(DBC case)</para>
        /// </summary>
        /// <param name="input">任意字符串</param>
        /// <returns>半角字符串</returns>
        ///<remarks>
        ///全角空格为12288，半角空格为32
        ///其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
        ///</remarks>
        public static string ToDBC(this string input)
        {
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 12288)
                {
                    c[i] = (char)32;
                    continue;
                }
                if (c[i] > 65280 && c[i] < 65375)
                    c[i] = (char)(c[i] - 65248);
            }
            return new string(c);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>半角转全角的函数(SBC case)</para>
        /// </summary>
        /// <param name="input">任意字符串</param>
        /// <returns>全角字符串</returns>
        ///<remarks>
        ///全角空格为12288，半角空格为32
        ///其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
        ///</remarks>       
        public static string ToSBC(this string input)
        {
            //半角转全角：
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 32)
                {
                    c[i] = (char)12288;
                    continue;
                }
                if (c[i] < 127)
                    c[i] = (char)(c[i] + 65248);
            }
            return new string(c);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>金额格式字符串转decimal，默认保留两位小数</para>
        /// </summary>
        /// <param name="input">金额格式字符串</param>
        ///  <param name="digits">精度(保留的几位小数)</param>
        /// <returns></returns>
        public static decimal MoneyStringToDecimal(this string input,int digits=2)
        {
            if(string.IsNullOrEmpty(input)) return Math.Round(0M, digits);

            string inputStr = input.Replace("$", "").Replace("¥","").Replace("￥", "").Replace(",", "");

           var flag= decimal.TryParse(inputStr, out decimal result);
            if (flag)
            {
                return Math.Round(result, digits);
            }
            else
                return Math.Round(0M, digits);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>获取hashCode</para>
        /// </summary>
        /// <param name="input"></param>
        /// <param name="values"></param>
        /// <returns></returns> 
        public static int ToHashCode(this string input, params object[] values)
        {
            string result = input;
            foreach (var value in values)
            {
                if (value != null)
                {
                    result += value.ToString();
                }
            }
            return result.GetHashCode();
        }
    }
}
