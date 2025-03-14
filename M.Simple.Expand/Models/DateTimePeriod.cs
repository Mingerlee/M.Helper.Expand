using System;

namespace M.Simple.Expand.Models
{
    /// <summary>
    /// <para>M.Simple.Expand扩展</para>
    /// <para>日期时间集合</para>
    /// </summary>
    public class DateTimePeriod
    {
        /// <summary>
        ///     构造
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <exception cref="ArgumentException"></exception>
        public DateTimePeriod(DateTime startTime, DateTime endTime)
        {
            if (startTime > endTime)
            {
                throw new ArgumentException("The end time must be greater than the start time!");
            }
            StartTime = startTime;
            EndTime = endTime;
        }
        /// <summary>
        ///     开始时间
        /// </summary>
        public DateTime StartTime { get;private set; }
        /// <summary>
        ///     结束时间
        /// </summary>
        public DateTime EndTime { get; private set; }
    }
    /// <summary>
    /// <para>M.Simple.Expand扩展</para>
    /// <para>日期集合</para>
    /// </summary>
    public class DatePeriod
    {
        /// <summary>
        ///     构造函数
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <exception cref="ArgumentException"></exception>
        public DatePeriod(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                throw new ArgumentException("The end date must be greater than the start date!");
            }
            _startDate =startDate;
            _endDate=endDate;
        }
        private readonly DateTime _startDate; // 私有字段
        private readonly DateTime _endDate; // 私有字段
        /// <summary>
        ///     开始日期
        /// </summary>
        public DateTime StartDate
        {
            get => _startDate.Date;
        }
        /// <summary>
        ///     结束日期
        /// </summary>
        public DateTime EndDate
        {
            get => _endDate.Date;
        }
    }
}
