using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace M.Helper.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// <para>数据转大写</para>
        /// <para>四舍五入后保留两位小数</para>
        /// </summary>
        /// <param name="num"> </param>
        /// <returns> </returns>
        public static string ConvertToChinese(this decimal num)
        {
            var s = num.ToString("#L#E#D#C#K#E#D#C#J#E#D#C#I#E#D#C#H#E#D#C#G#E#D#C#F#E#D#C#.0B0A");
            var d = Regex.Replace(s, @"((?<=-|^)[^1-9]*)|((?'z'0)[0A-E]*((?=[1-9])|(?'-z'(?=[F-L\.]|$))))|((?'b'[F-L])(?'z'0)[0A-L]*((?=[1-9])|(?'-z'(?=[\.]|$))))", "${b}${z}");
            var r = Regex.Replace(d, ".", m => "负圆空零壹贰叁肆伍陆柒捌玖空空空空空空空分角拾佰仟万亿兆京垓秭穰"[m.Value[0] - '-'].ToString());
            return r.EndsWith('圆') ? r + "整" : r;
        }
        /// <summary>
        ///     四舍五入
        /// </summary>
        /// <param name="num"></param>
        /// <param name="decimals">保留位数</param>
        /// <returns></returns>
        public static decimal Round(this decimal num, int decimals=0)
        {
            return Math.Round(num, decimals,MidpointRounding.AwayFromZero);
        }
        /// <summary>
        ///     四舍五入
        /// </summary>
        /// <param name="num"></param>
        /// <param name="decimals">保留位数</param>
        /// <returns></returns>
        public static decimal Round(this decimal? num, int decimals = 0)
        {
            if (num.HasValue)
            {
                return Math.Round(num.GetValueOrDefault(), decimals, MidpointRounding.AwayFromZero);
            }
            return 0;
        }
        /// <summary>
        ///     向上进位取整
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static decimal Ceiling(this decimal num)
        {
            return Math.Ceiling(num);
        }
        /// <summary>
        ///     向下舍位取整
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static decimal Floor(this decimal num)
        {
            return Math.Floor(num);
        }
        /// <summary>
        ///     金额格式化
        /// </summary>
        /// <param name="num"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string FormatMoney(this decimal num, FormatMoneyEnum type= FormatMoneyEnum.C0)
        {
            if (num == 0) return "0";
            return type switch
            {
                FormatMoneyEnum.C0 => string.Format("{0:###,###}", num),
                FormatMoneyEnum .C0_zh_CN=> num.ToString("c0",new CultureInfo("zh-CN")),
                FormatMoneyEnum.C0_en_US => num.ToString("c0", new CultureInfo("en-US")),
                FormatMoneyEnum.C1 => string.Format("{0:#,0.#}", num),
                FormatMoneyEnum.C1_zh_CN => num.ToString("c1", new CultureInfo("zh-CN")),
                FormatMoneyEnum.C1_en_US => num.ToString("c1", new CultureInfo("en-US")),
                FormatMoneyEnum.C2 => string.Format("{0:#,0.##}", num),
                FormatMoneyEnum.C2_zh_CN => num.ToString("c2", new CultureInfo("zh-CN")),
                FormatMoneyEnum.C2_en_US => num.ToString("c2", new CultureInfo("en-US")),
                FormatMoneyEnum.C3 => string.Format("{0:#,0.###}", num),
                FormatMoneyEnum.C3_zh_CN => num.ToString("c3", new CultureInfo("zh-CN")),
                FormatMoneyEnum.C3_en_US => num.ToString("c3", new CultureInfo("en-US")),
                FormatMoneyEnum.C4 => string.Format("{0:#,0.####}", num),
                FormatMoneyEnum.C4_zh_CN => num.ToString("c4", new CultureInfo("zh-CN")),
                FormatMoneyEnum.C4_en_US => num.ToString("c4", new CultureInfo("en-US")),
                _ => string.Format("{0:###,###}", num)
            };
        }
        /// <summary>
        ///     金额格式化
        /// </summary>
        /// <param name="num"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string FormatMoney(this decimal? num, FormatMoneyEnum type = FormatMoneyEnum.C0)
        {
            if (num == 0) return "0";
            if(!num.HasValue) return "0";
            decimal number =num.GetValueOrDefault();
            return type switch
            {
                FormatMoneyEnum.C0 => string.Format("{0:###,###}", number),
                FormatMoneyEnum.C0_zh_CN => number.ToString("c0", new CultureInfo("zh-CN")),
                FormatMoneyEnum.C0_en_US => number.ToString("c0", new CultureInfo("en-US")),
                FormatMoneyEnum.C1 => string.Format("{0:#,0.#}", number),
                FormatMoneyEnum.C1_zh_CN => number.ToString("c1", new CultureInfo("zh-CN")),
                FormatMoneyEnum.C1_en_US => number.ToString("c1", new CultureInfo("en-US")),
                FormatMoneyEnum.C2 => string.Format("{0:#,0.##}", number),
                FormatMoneyEnum.C2_zh_CN => number.ToString("c2", new CultureInfo("zh-CN")),
                FormatMoneyEnum.C2_en_US => number.ToString("c2", new CultureInfo("en-US")),
                FormatMoneyEnum.C3 => string.Format("{0:#,0.###}", number),
                FormatMoneyEnum.C3_zh_CN => number.ToString("c3", new CultureInfo("zh-CN")),
                FormatMoneyEnum.C3_en_US => number.ToString("c3", new CultureInfo("en-US")),
                FormatMoneyEnum.C4 => string.Format("{0:#,0.####}", number),
                FormatMoneyEnum.C4_zh_CN => number.ToString("c4", new CultureInfo("zh-CN")),
                FormatMoneyEnum.C4_en_US => number.ToString("c4", new CultureInfo("en-US")),
                _ => string.Format("{0:###,###}", number)
            };
        }
    }
}
