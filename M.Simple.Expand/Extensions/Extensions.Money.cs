using M.Simple.Expand.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        [GeneratedRegex(@"((?<=-|^)[^1-9]*)|((?'z'0)[0A-E]*((?=[1-9])|(?'-z'(?=[F-L\.]|$))))|((?'b'[F-L])(?'z'0)[0A-L]*((?=[1-9])|(?'-z'(?=[\.]|$))))")]
        private static partial Regex MyRegex();
        [GeneratedRegex(".")]
        private static partial Regex MyRegex1();
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>数据转中文大写：四舍五入后保留两位小数</para>
        /// </summary>
        /// <param name="num"> </param>
        /// <returns> </returns>
        public static string ToChinese(this decimal num)
        {
            if (num == 0)
            {
                return "零圆";
            }
            var s = num.ToString("#L#E#D#C#K#E#D#C#J#E#D#C#I#E#D#C#H#E#D#C#G#E#D#C#F#E#D#C#.0B0A");
            var d = MyRegex().Replace(s, "${b}${z}");
            var r = MyRegex1().Replace(d, m => "负圆空零壹贰叁肆伍陆柒捌玖空空空空空空空分角拾佰仟万亿兆京垓秭穰"[m.Value[0] - '-'].ToString());
            if (num < 0) 
            {
                return "负"+( r.EndsWith('圆') ? r + "整" : r);
            }
            return r.EndsWith('圆') ? r + "整" : r;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>四舍五入</para>
        /// </summary>
        /// <param name="num"></param>
        /// <param name="digits">精度</param>
        /// <returns></returns>
        public static decimal ToRound(this decimal num, int digits = 0)
        {
            return Math.Round(num, digits, MidpointRounding.AwayFromZero);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>四舍五入</para>
        /// </summary>
        /// <param name="num"></param>
        /// <param name="digits">精度</param>
        /// <returns>null值默认返回0</returns>
        public static decimal ToRound(this decimal? num, int digits = 0)
        {
            if (num.HasValue)
            {
                return Math.Round(num.Value, digits, MidpointRounding.AwayFromZero);
            }
            return 0;
        }
        /// <summary>
        ///<para>M.Simple.Expand扩展</para>
        /// <para>金额格式化</para>
        /// </summary>
        /// <param name="num"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ToFormatMoney(this decimal num, FormatMoneyEnum type = FormatMoneyEnum.C0)
        {
            return type switch
            {
                FormatMoneyEnum.C0 => string.Format("{0:###,###}", num),
                FormatMoneyEnum.C0_zh_CN => num.ToString("c0", new CultureInfo("zh-CN")),
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
        ///<para>M.Simple.Expand扩展</para>
        ///<para>金额格式化</para>
        /// </summary>
        /// <param name="num"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ToFormatMoney(this decimal? num, FormatMoneyEnum type = FormatMoneyEnum.C0)
        {
            decimal number = num??0;
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
        /// <summary>
        /// 按规则向上取整：保留两位小数，第三位及以后向上进一
        /// 212.0001 → 212.00
        /// 212.001  → 212.01
        /// 212.000  → 212.00
        /// </summary>
        public static decimal CeilingSpecial(this decimal value, int digits = 2)
        {
            // 计算放大倍数 10^n
            decimal multiplier = (decimal)Math.Pow(10, digits);
            decimal scaled = value * multiplier;
            var fraction = IsNextDecimalDigitZero(value, digits);

            if (fraction)
            {
                // 是0，直接舍掉
                return Math.Floor(scaled) / multiplier;
            }
            else
            {
                // 有小数部分 → 向上取整
                return Math.Ceiling(scaled) / multiplier;
            }
        }
        /// <summary>
        /// 判断 第 (decimalPlaces + 1) 位小数 是否等于 0
        /// 例：decimalPlaces = 2 → 判断第 3 位小数是不是 0
        /// </summary>
        /// <returns>true=第N+1位是0，false=第N+1位非0</returns>
        private static bool IsNextDecimalDigitZero(decimal value, int decimalPlaces)
        {
            if (decimalPlaces < 0)
                throw new ArgumentOutOfRangeException(nameof(decimalPlaces));

            // 放大到要判断的那一位
            decimal multiplier = (decimal)Math.Pow(10, decimalPlaces + 1);
            decimal scaled = value * multiplier;

            // 取整数部分的最后一位，就是第 N+1 位小数
            int digit = (int)(Math.Floor(scaled) % 10);

            return digit == 0;
        }
    }
}
