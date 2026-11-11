using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand.Enums
{
    /// <summary>
    ///     
    /// </summary>
    public enum DiffMonthWayEnum
    {
        /// <summary>
        ///     自然月差，忽略日；方向为结束日期减开始日期
        /// </summary>
        简单 = 1,
        /// <summary>
        ///     完整月差，按较早日期AddMonths确定边界，缺失对应日时取月末
        /// </summary>
        精确 = 2,
        /// <summary>
        ///     跨过的自然月边界数；同月为0，与简单模式一致
        /// </summary>
        跨月 = 3,
    }
}
