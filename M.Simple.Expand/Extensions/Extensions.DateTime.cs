using M.Simple.Expand.Enums;
using M.Simple.Expand.Helper;
using M.Simple.Expand.Models;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        private static readonly DateTime defaultDate = new(1900,01,01);
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>默认日期格式："yyyy/MM/dd"</para>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToDefaultDate(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy/MM/dd");
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>默认日期格式："yyyy/MM/dd"</para>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns>如果为null返回string.Empty</returns>
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
        /// <para>M.Simple.Expand扩展</para>
        /// <para>默认日期格式："yyyy/MM/dd"</para>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="defaultDateTime">默认日期</param>
        /// <returns>如果为null返回默认日期</returns>
        public static string ToDefaultDate(this DateTime? dateTime,DateTime defaultDateTime)
        {
            if (dateTime.HasValue)
            {
                return dateTime.Value.ToDefaultDate();
            }
            else
            {
                return defaultDateTime.ToDefaultDate();
            }
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>日期格式："yyyy年MM月dd日"</para>
        /// </summary>
        /// <param name="dateTime">日期</param>
        public static string ToChineseDateString(this DateTime dateTime)
        {
            return string.Format("{0}年{1}月{2}日", dateTime.Year, dateTime.Month, dateTime.Day);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>日期格式："yyyy年MM月dd日"</para>
        /// </summary>
        /// <param name="dateTime">日期</param>
        /// <returns>如果为null返回string.Empty</returns>
        public static string ToChineseDateString(this DateTime? dateTime)
        {
            if (dateTime.HasValue)
            {
                return string.Format("{0}年{1}月{2}日", dateTime.Value.Year, dateTime.Value.Month, dateTime.Value.Day);
            }
            else
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>日期格式："yyyy年MM月dd日"</para>
        /// </summary>
        /// <param name="dateTime">日期</param>
        /// <param name="defaultDateTime">默认日期</param>
        /// <returns>如果为null返回默认日期</returns>
        public static string ToChineseDateString(this DateTime? dateTime, DateTime defaultDateTime)
        {
            if (dateTime.HasValue)
            {
                return string.Format("{0}年{1}月{2}日", dateTime.Value.Year, dateTime.Value.Month, dateTime.Value.Day);
            }
            else
            {
                return string.Format("{0}年{1}月{2}日", defaultDateTime.Year, defaultDateTime.Month, defaultDateTime.Day);
            }
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>日期时间格式："yyyy年MM月dd日 HH时mm分"</para>
        /// </summary>
        /// <param name="dateTime">日期</param>
        /// <param name="removeSecond">是否移除秒</param>
        /// <returns></returns>
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
        /// <para>M.Simple.Expand扩展</para>
        /// <para>日期时间格式："yyyy年MM月dd日 HH时mm分"</para>
        /// </summary>
        /// <param name="dateTime">日期</param>
        /// <param name="removeSecond">是否移除秒</param>
        /// <returns>如果为null返回string.Empty</returns>
        public static string ToChineseDateTimeString(this DateTime? dateTime, bool removeSecond = false)
        {
            if (dateTime == null)
                return string.Empty;
            return ToChineseDateTimeString(dateTime.Value, removeSecond);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>日期时间格式："yyyy年MM月dd日 HH时mm分"</para>
        /// </summary>
        /// <param name="dateTime">日期</param>
        /// <param name="defaultDateTime">默认日期</param>
        /// <param name="removeSecond">是否移除秒</param>
        /// <returns>如果为null返回默认日期</returns>
        public static string ToChineseDateTimeString(this DateTime? dateTime, DateTime defaultDateTime, bool removeSecond = false)
        {
            if (dateTime == null)
                return ToChineseDateTimeString(defaultDateTime, removeSecond);
            else
                return ToChineseDateTimeString(dateTime.Value, removeSecond);
        }

        /// <summary>
        ///<para>M.Simple.Expand扩展</para>
        ///<para>时间戳编码</para>
        /// </summary>
        /// <param name="time"></param>
        /// <returns>yyyyMMddHHmmssfff</returns>
        public static string ToTimesTampCode(this DateTime time)
        {
            return time.ToString("yyyyMMddHHmmssfff");
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>距离当前时间过去多久</para>
        /// </summary>
        /// <param name="time"></param>
        /// <param name="dateTime"></param>
        /// <param name="language">语言：中文；英文</param>
        /// <returns></returns>
        public static string ToPastCurrentTime(this DateTime time, DateTime? dateTime = null, LanguageEnum language = LanguageEnum.zh_CN)
        {
            var ts = new TimeSpan(dateTime.GetValueOrDefault(DateTime.Now).Ticks - time.Ticks);
            double delta = ts.TotalSeconds;
            List<string> str = LanguageStr(language);
            if (delta > 0)
            {
                if (delta < 60) // 60 (seconds)
                {
                    return ts.Seconds == 1 ? str[0] : ts.Seconds.ToString() + str[1];
                }
                else if (delta < 120) //2 (minutes) * 60 (seconds)
                {
                    return str[2];
                }
                else if (delta < 2700) // 45 (minutes) * 60 (seconds)
                {
                    return ts.Minutes.ToString() + str[3];
                }
                else if (delta < 5400) // 90 (minutes) * 60 (seconds)
                {
                    return str[4];
                }
                else if (delta < 86400) // 24 (hours) * 60 (minutes) * 60 (seconds)
                {
                    int hours = ts.Hours;
                    if (hours == 1)
                        hours = 2;
                    return hours.ToString() + str[5];
                }
                else if (delta < 172800) // 48 (hours) * 60 (minutes) * 60 (seconds)
                {
                    return str[6];
                }
                else if (delta < 2592000) // 30 (days) * 24 (hours) * 60 (minutes) * 60 (seconds)
                {
                    return ts.Days.ToString() + str[7];
                }
                else if (delta < 31104000) // 12 (months) * 30 (days) * 24 (hours) * 60 (minutes) * 60 (seconds)
                {
                    int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                    return months <= 1 ? str[8] : months.ToString() + str[9];
                }
                else
                {
                    int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                    return years <= 1 ? str[10] : years.ToString() + str[11];
                }
            }
            else
            {
                return time.ToString("yyyy/MM/dd HH:mm:ss");
            }
        }
        /// <summary>
        ///<para>M.Simple.Expand扩展</para>
        ///<para>时间段是否有重叠</para>
        /// </summary>
        /// <param name="periods">时间段集合</param>
        /// <returns>有重合返回true，否则返回false</returns>
        public static bool IsOverlap(this List<DateTimePeriod> periods)
        {
            // 不足两个时间段不可能重叠。
            if (periods == null || periods.Count < 2) return false;

            periods.Sort((x, y) => x.StartTime.CompareTo(y.StartTime));

            DateTime farthestEnd = periods[0].EndTime;

            for (int i = 1; i < periods.Count; i++)
            {
                if (periods[i].StartTime < farthestEnd)
                    return true; // 发生重叠

                if (periods[i].EndTime > farthestEnd)
                    farthestEnd = periods[i].EndTime;
            }

            return false; // 没有发生重叠
        }
        /// <summary>
        ///<para>M.Simple.Expand扩展</para>
        ///<para>日期段是否有重叠</para>
        ///<para>开始日期开区间，结束日期闭区间</para>
        /// </summary>
        /// <param name="periods">时间段集合</param>
        /// <returns>有重合返回true，否则返回false</returns>
        public static bool IsOverlap(this List<DatePeriod> periods)
        {
            // 不足两个时间段不可能重叠。
            if (periods == null || periods.Count < 2) return false;

            periods.Sort((x, y) => x.StartDate.CompareTo(y.StartDate));

            DateTime farthestEnd = periods[0].EndDate;

            for (int i = 1; i < periods.Count; i++)
            {
                if (periods[i].StartDate <= farthestEnd)
                    return true; // 发生重叠

                if (periods[i].EndDate > farthestEnd)
                    farthestEnd = periods[i].EndDate;
            }

            return false; // 没有发生重叠
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>依据出生日期计算年龄</para>
        /// </summary>
        /// <param name="birthDate"></param>
        /// <returns></returns>
        public static int CalculateAge(this DateTime birthDate)
        {
            var currentDate = DateTime.Now.Date;
            int age = currentDate.Year - birthDate.Year;
            if (currentDate.Month < birthDate.Month || (currentDate.Month == birthDate.Month && currentDate.Day < birthDate.Day))
            {
                age--;
            }
            return age;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>获取日期（不含时分秒）</para>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="defaultDateTime">默认日期</param>
        /// <returns></returns>
        public static DateTime GetDate(this DateTime? dateTime, DateTime? defaultDateTime=null)
        {
            return dateTime.GetValueOrDefault(defaultDateTime??defaultDate).Date;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>转换为农历日期</para>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToLunarCalendarDate(this DateTime dateTime)
        {
            ChineseCalendarHelper chineseCalendar = new(dateTime);
            return chineseCalendar.ChineseDateString;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>转换为农历日期</para>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToLunarCalendarDate(this DateTime? dateTime)
        {
            dateTime ??= DateTime.Now;

            ChineseCalendarHelper chineseCalendar = new(dateTime.Value);
            return chineseCalendar.ChineseDateString;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>转换为农历日期</para>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="defaultDateTime">默认日期</param>
        /// <returns></returns>
        public static string ToLunarCalendarDate(this DateTime? dateTime, DateTime defaultDateTime)
        {
            dateTime ??= defaultDateTime;

            ChineseCalendarHelper chineseCalendar = new(dateTime.Value);
            return chineseCalendar.ChineseDateString;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>获取日期 下一天</para>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime NextDay(this DateTime dateTime)
        {
            return dateTime.AddDays(1);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>获取日期 下一天</para>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime NextDay(this DateTime? dateTime)
        {
            return dateTime.GetValueOrDefault(defaultDate).AddDays(1);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>获取日期 下一天</para>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="defaultDateTime">默认日期</param>
        /// <returns></returns>
        public static DateTime NextDay(this DateTime? dateTime, DateTime defaultDateTime)
        {
            return dateTime.GetValueOrDefault(defaultDateTime).AddDays(1);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>获取日期 上一天</para>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime PervDay(this DateTime dateTime)
        {
            return dateTime.AddDays(-1);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>获取日期 上一天</para>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime PervDay(this DateTime? dateTime)
        {
            return dateTime.GetValueOrDefault(defaultDate).AddDays(-1);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>获取日期 上一天</para>
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="defaultDateTime"></param>
        /// <returns></returns>
        public static DateTime PervDay(this DateTime? dateTime, DateTime defaultDateTime)
        {
            return dateTime.GetValueOrDefault(defaultDateTime).AddDays(-1);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        ///<para> 计算日期差</para>
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime">结束日期</param>
        /// <param name="includeToday">是否包含当天</param>
        /// <returns>天</returns>
        public static int DiffDays(this DateTime startTime, DateTime endTime, bool includeToday = false)
        {
            TimeSpan daysSpan = new(endTime.Date.Ticks - startTime.Date.Ticks);
            return includeToday ? daysSpan.Days + 1 : daysSpan.Days;
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para> 计算月份差</para>
        /// </summary>
        /// <param name="startTime">结束日期</param>
        /// <param name="endTime"></param>
        /// <param name="diffMonthWay">计算方式：简单、精确、跨月</param>
        /// <remarks>
        ///     <para> 简单整月差值：忽略具体日期，仅计算年份和月份差</para>
        ///     <para> 精确整月差值：只有当日期也相同才认为是整月</para>
        ///     <para> 跨月即计为一个月：只要进入下一个月即计为1个月</para>
        /// </remarks>
        /// <returns></returns>
        public static int DiffMonth(this DateTime startTime, DateTime endTime, DiffMonthWayEnum diffMonthWay= DiffMonthWayEnum.简单)
        {
            var baseMonth = ((startTime.Year - endTime.Year) * 12) + startTime.Month - endTime.Month;
            switch (diffMonthWay)
            {
                case DiffMonthWayEnum.简单:return baseMonth;
                case DiffMonthWayEnum.精确:if (startTime.Day < endTime.Day) baseMonth--;return baseMonth;
                case DiffMonthWayEnum.跨月: if (startTime.Day >= endTime.Day) baseMonth++; return baseMonth;
                default:return -1;
            }
            //return diffMonthWay switch
            //{
            //    DiffMonthWayEnum.简单 => baseMonth,
            //    DiffMonthWayEnum.精确 => startTime.Day < endTime.Day ? baseMonth-- : baseMonth,
            //    DiffMonthWayEnum.跨月 => startTime.Day >= endTime.Day ? baseMonth++ : baseMonth,
            //    _ => -1,
            //};
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para> 获取当月第一天</para>
        /// </summary>
        public static DateTime GetFirstDayOfMonth(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1);
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para> 获取当月最后一天</para>
        /// </summary>
        public static DateTime GetLastDayOfMonth(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, DateTime.DaysInMonth(dateTime.Year, dateTime.Month));
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para> 判断是否为今天</para>
        /// </summary>
        public static bool IsToday(this DateTime dt)
        {
            return dt.Date == DateTime.Today;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para> 判断是否为昨天</para>
        /// </summary>
        public static bool IsYesterday(this DateTime dt)
        {
            return dt.Date == DateTime.Today.AddDays(-1);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para> 判断是否为明天</para>
        /// </summary>
        public static bool IsTomorrow(this DateTime dt)
        {
            return dt.Date == DateTime.Today.AddDays(1);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para> 判断是否为周末（周六/周日）</para>
        /// </summary>
        public static bool IsWeekend(this DateTime dt)
        {
            return dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>判断是否为工作日（周一至周五）</para>
        /// </summary>
        public static bool IsWorkday(this DateTime dt)
        {
            return !dt.IsWeekend();
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>取本周的第一天（周一）</para>
        /// </summary>
        public static DateTime FirstDayOfWeek(this DateTime dt)
        {
            // 调整周一开始（默认DayOfWeek.Sunday是周日）
            int diff = dt.DayOfWeek - DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            return dt.AddDays(-diff).Date;
        }
        /// <summary>
        ///<para>M.Simple.Expand扩展</para>
        ///<para>取本周的最后一天（周日）</para>
        /// </summary>
        public static DateTime LastDayOfWeek(this DateTime dt)
        {
            return dt.FirstDayOfWeek().AddDays(6);
        }
        private static List<string> LanguageStr(LanguageEnum language)
        {
            List<string> cn = ["1秒以前", "秒以前", "1分钟以前", "分钟以前", "1小时以前", "小时以前", "昨天", "天以前", "1个月以前", "个月以前", "1年以前", "年以前"];
            List<string> en = ["1 second ago", " seconds ago", "a minute ago", " minutes ago", "an hour ago", "hours ago", "yesterday", "days ago", "a month ago", "months ago", "a year ago", "years ago"];
            return language switch
            {
                LanguageEnum.zh_CN => cn,
                LanguageEnum.en_US => en,
                _ => cn,
            };
        }
    }
}
