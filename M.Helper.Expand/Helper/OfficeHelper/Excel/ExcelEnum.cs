using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Helper.Expand
{
    /// <summary>
    ///     统计公式类型
    /// </summary>
    public enum FormulaType
    {
        /// <summary>
        ///     求和
        /// </summary>
        SUM = 1,
        /// <summary>
        ///     求平均值
        /// </summary>
        AVERAGE=2,
        /// <summary>
        ///     计数
        /// </summary>
        COUNT=3,
        /// <summary>
        ///     最大值
        /// </summary>
        MAX=4,
        /// <summary>
        ///     最小值
        /// </summary>
        MIN=5,
    }
    /// <summary>
    ///     单元格公式类型
    /// </summary>
    public enum CellFormulaType
    {
        /// <summary>
        ///     四舍五入
        /// </summary>
        ROUND = 6,
        /// <summary>
        ///     绝对值
        /// </summary>
        ABS=7,
    }
}
