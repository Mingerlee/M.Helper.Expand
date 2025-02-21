using System;
using System.Text;

namespace M.Helper.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="type">
        ///     101:yyyy-MM-dd HH:mm
        ///     102:yyyy/MM/dd HH:mm
        ///     103:yyyy-MM-dd HH:mm:ss
        ///     104:yyyy/MM/dd HH:mm:ss
        ///     201:yyyy-MM-dd
        ///     202:yyyy/MM/dd
        ///     301:xxxx年xx月xx日
        ///     302:xxxx年xx月xx日 xx时xx分
        ///     303:xxxx年xx月xx日 xx时xx分xx秒
        /// </param>
        /// <returns></returns>
        public static string ToDateTimeString(this DateTime dateTime, int? type)
        {
            return type switch
            {
                101 => dateTime.ToString("yyyy-MM-dd HH:mm"),
                102 => dateTime.ToString("yyyy/MM/dd HH:mm"),
                103 => dateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                104 => dateTime.ToString("yyyy/MM/dd HH:mm:ss"),
                201 => dateTime.ToString("yyyy-MM-dd"),
                202 => dateTime.ToString("yyyy/MM/dd"),
                301=> $"{dateTime.Year}年{dateTime.Month}月{dateTime.Day}日",
                302 => $"{dateTime.Year}年{dateTime.Month}月{dateTime.Day}日 {dateTime.Hour}时{dateTime.Minute}分",
                303 => $"{dateTime.Year}年{dateTime.Month}月{dateTime.Day}日 {dateTime.Hour}时{dateTime.Minute}分{dateTime.Second}秒",
                _ => dateTime.ToString("yyyy-MM-dd HH:mm"),
            };
        }
        /// <summary>
        /// 获取格式化字符串，带时分秒，格式："yyyy-MM-dd HH:mm:ss"
        /// </summary>
        /// <param name="dateTime">日期</param>
        /// <param name="removeSecond">是否移除秒</param>
        public static string ToDateTimeString(this DateTime dateTime, bool removeSecond = false)
        {
            return removeSecond ? dateTime.ToString("yyyy-MM-dd HH:mm") : dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 获取格式化字符串，带时分秒，格式："yyyy-MM-dd HH:mm:ss"
        /// </summary>
        /// <param name="dateTime">日期</param>
        /// <param name="removeSecond">是否移除秒</param>
        public static string ToDateTimeString(this DateTime? dateTime, bool removeSecond = false)
        {
            if (dateTime == null)
                return string.Empty;
            return ToDateTimeString(dateTime.Value, removeSecond);
        }

        /// <summary>
        /// 获取格式化字符串，不带时分秒，格式："yyyy-MM-dd"
        /// </summary>
        /// <param name="dateTime">日期</param>
        public static string ToDateString(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// 获取格式化字符串，不带时分秒，格式："yyyy-MM-dd"
        /// </summary>
        /// <param name="dateTime">日期</param>
        public static string ToDateString(this DateTime? dateTime)
        {
            if (dateTime == null)
                return string.Empty;
            return ToDateString(dateTime.Value);
        }

        /// <summary>
        /// 获取格式化字符串，不带年月日，格式："HH:mm:ss"
        /// </summary>
        /// <param name="dateTime">日期</param>
        public static string ToTimeString(this DateTime dateTime)
        {
            return dateTime.ToString("HH:mm:ss");
        }

        /// <summary>
        /// 获取格式化字符串，不带年月日，格式："HH:mm:ss"
        /// </summary>
        /// <param name="dateTime">日期</param>
        public static string ToTimeString(this DateTime? dateTime)
        {
            if (dateTime == null)
                return string.Empty;
            return ToTimeString(dateTime.Value);
        }

        /// <summary>
        /// 获取格式化字符串，带毫秒，格式："yyyy-MM-dd HH:mm:ss.fff"
        /// </summary>
        /// <param name="dateTime">日期</param>
        public static string ToMillisecondString(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
        /// <summary>
        /// 获取格式化字符串，带毫秒，格式："yyyy-MM-dd HH:mm"
        /// </summary>
        /// <param name="dateTime">日期</param>
        public static string ToMinutesString(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm");
        }
        /// <summary>
        /// 获取格式化字符串，带毫秒，格式："yyyy-MM-dd HH:mm:ss.fff"
        /// </summary>
        /// <param name="dateTime">日期</param>
        public static string ToMillisecondString(this DateTime? dateTime)
        {
            if (dateTime == null)
                return string.Empty;
            return ToMillisecondString(dateTime.Value);
        }

        /// <summary>
        /// 获取格式化字符串，不带时分秒，格式："yyyy年MM月dd日"
        /// </summary>
        /// <param name="dateTime">日期</param>
        public static string ToChineseDateString(this DateTime dateTime)
        {
            return string.Format("{0}年{1}月{2}日", dateTime.Year, dateTime.Month, dateTime.Day);
        }

        /// <summary>
        /// 获取格式化字符串，带时分秒，格式："yyyy年MM月dd日 HH时mm分"
        /// </summary>
        /// <param name="dateTime">日期</param>
        /// <param name="removeSecond">是否移除秒</param>
        public static string ToChineseDateTimeString(this DateTime dateTime, bool removeSecond = false)
        {
            StringBuilder result = new StringBuilder();
            result.AppendFormat("{0}年{1}月{2}日", dateTime.Year, dateTime.Month, dateTime.Day);
            result.AppendFormat(" {0}时{1}分", dateTime.Hour, dateTime.Minute);
            if (removeSecond == false)
                result.AppendFormat("{0}秒", dateTime.Second);
            return result.ToString();
        }

        /// <summary>
        /// 获取格式化字符串，带时分秒，格式："yyyy年MM月dd日 HH时mm分"
        /// </summary>
        /// <param name="dateTime">日期</param>
        /// <param name="removeSecond">是否移除秒</param>
        public static string ToChineseDateTimeString(this DateTime? dateTime, bool removeSecond = false)
        {
            if (dateTime == null)
                return string.Empty;
            return ToChineseDateTimeString(dateTime.Value, removeSecond);
        }
        /// <summary>
        ///     获取格式化字符串，格式："yyyy/MM/dd"
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToDefaultDate(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy/MM/dd");
        }
        /// <summary>
        ///     获取格式化字符串，格式："yyyy/MM/dd"
        ///     如果为null返回string.Empty
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToDefaultDate(this DateTime? dateTime)
        {
            if (dateTime.HasValue)
            {
                return dateTime.Value.ToDefaultDate();
            }
            else
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// 获取描述
        /// </summary>
        /// <param name="span">时间间隔</param>
        public static string Description(this TimeSpan span)
        {
            StringBuilder result = new StringBuilder();
            if (span.Days > 0)
                result.AppendFormat("{0}天", span.Days);
            if (span.Hours > 0)
                result.AppendFormat("{0}小时", span.Hours);
            if (span.Minutes > 0)
                result.AppendFormat("{0}分", span.Minutes);
            if (span.Seconds > 0)
                result.AppendFormat("{0}秒", span.Seconds);
            if (span.Milliseconds > 0)
                result.AppendFormat("{0}毫秒", span.Milliseconds);
            if (result.Length > 0)
                return result.ToString();
            return $"{span.TotalSeconds * 1000}毫秒";
        }

        /// <summary>
        /// 转成 *天*小时*分钟*秒 的格式
        /// </summary>
        /// <param name="timespan"></param>
        /// <returns></returns>
        public static string ToHumanString(this TimeSpan timespan)
        {
            var sb = new StringBuilder();

            var seconds = timespan.TotalSeconds;

            if (seconds >= 86400)
            {
                var days = seconds / 86400;
                sb.Append(days);
                sb.Append("天");

                seconds -= days * 86400;
            }
            if (seconds > 3600)
            {
                var hours = seconds / 3600;
                sb.Append(hours);
                sb.Append("小时");

                seconds -= hours * 3600;
            }
            if (seconds > 60)
            {
                var minutes = seconds / 60;
                sb.Append(minutes);
                sb.Append("分钟");

                seconds -= minutes * 60;
            }
            if (seconds > 0)
            {
                sb.Append(seconds);
                sb.Append("秒");
            }

            return sb.ToString();
        }
        /// <summary>
        /// 距离当前时间过去多久
        /// </summary>
        /// <param name="time"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToPastCurrentTime(this DateTime time,DateTime? dateTime=null)
        {
            string result = "";

            var ts = dateTime.HasValue? new TimeSpan(dateTime.GetValueOrDefault().Ticks - time.Ticks):new TimeSpan(DateTime.Now.Ticks - time.Ticks);
            double delta = ts.TotalSeconds;

            if (delta > 0)
            {
                if (delta < 60) // 60 (seconds)
                {
                    result = ts.Seconds == 1 ? "1秒以前" : ts.Seconds + "秒以前";
                }
                else if (delta < 120) //2 (minutes) * 60 (seconds)
                {
                    result = "1分钟以前";
                }
                else if (delta < 2700) // 45 (minutes) * 60 (seconds)
                {
                    result = ts.Minutes + "分钟以前";
                }
                else if (delta < 5400) // 90 (minutes) * 60 (seconds)
                {
                    result = "1小时以前";
                }
                else if (delta < 86400) // 24 (hours) * 60 (minutes) * 60 (seconds)
                {
                    int hours = ts.Hours;
                    if (hours == 1)
                        hours = 2;
                    result = hours + "小时以前";
                }
                else if (delta < 172800) // 48 (hours) * 60 (minutes) * 60 (seconds)
                {
                    result = "昨天";
                }
                else if (delta < 2592000) // 30 (days) * 24 (hours) * 60 (minutes) * 60 (seconds)
                {
                    result = ts.Days + "天以前";
                }
                else if (delta < 31104000) // 12 (months) * 30 (days) * 24 (hours) * 60 (minutes) * 60 (seconds)
                {
                    int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                    result = months <= 1 ? "1个月以前" : months + "个月以前";
                }
                else
                {
                    int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                    result = years <= 1 ? "1年以前" : years + "年以前";
                }
            }
            else
            {
                result = time.ToDateTimeString(104);
            }
            return result;
        }
        /// <summary>
        ///     时间戳编码：yyyyMMddHHmmssfff
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ToDateTimeCode(this DateTime time)
        {
            return time.ToString("yyyyMMddHHmmssfff");
        }
    }
}
