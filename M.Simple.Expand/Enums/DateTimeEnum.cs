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
        ///     简单整月差值
        /// </summary>
        简单 = 1,
        /// <summary>
        ///     精确整月差值
        /// </summary>
        精确 = 2,
        /// <summary>
        ///     跨月即计为一个月
        /// </summary>
        跨月 = 3,
    }
}
