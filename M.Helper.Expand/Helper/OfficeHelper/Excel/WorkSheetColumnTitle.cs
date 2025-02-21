using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Helper.Expand
{
    /// <summary>
    ///     列头
    /// </summary>
    public class WorkSheetColumnTitle
    {
        /// <summary>
        ///    列头名称
        /// </summary>
        public string ColumnTitle { get; set; }

        /// <summary>
        ///     列索引
        /// </summary>
        public int? ColumnIndex { get; set; }
        /// <summary>
        ///     公式类型
        /// </summary>
        public FormulaType? ColumnFormulaType { get; set; }
    }
}
