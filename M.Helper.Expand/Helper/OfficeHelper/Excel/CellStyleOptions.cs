using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Helper.Expand
{
    /// <summary>
    ///     单元格格式
    /// </summary>
    public class CellStyleOptions
    {
        /// <summary>
        ///     水平对齐
        /// </summary>
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;
        /// <summary>
        ///     垂直对齐
        /// </summary>
        public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;
        /// <summary>
        ///     是否自动换行
        /// </summary>
        public bool WrapText { get; set; } = false;
        /// <summary>
        ///     字体大小
        /// </summary>
        public int FontHeightInPoints { get; set; } = 12;
        /// <summary>
        ///     字体名称
        /// </summary>
        public string FontName { get; set; } = "宋体";
        /// <summary>
        ///     是否粗体
        /// </summary>
        public bool IsBold { get; set; } = false;
        /// <summary>
        ///     字体颜色
        /// </summary>
        public short Color { get; set; } = IndexedColors.Black.Index;
        /// <summary>
        ///     左边框格式
        /// </summary>
        public BorderStyle BorderLeft { get; set; } = BorderStyle.None;
        /// <summary>
        ///     右边框格式
        /// </summary>
        public BorderStyle BorderRight { get; set; } = BorderStyle.None;
        /// <summary>
        ///     上边框格式
        /// </summary>
        public BorderStyle BorderTop { get; set; } = BorderStyle.None;
        /// <summary>
        ///     下边框格式
        /// </summary>
        public BorderStyle BorderBottom { get; set; } = BorderStyle.None;
        /// <summary>
        ///     左边框颜色
        /// </summary>
        public short LeftBorderColor { get; set; } = IndexedColors.Black.Index;
        /// <summary>
        ///     右边框颜色
        /// </summary>
        public short RightBorderColor { get; set; } = IndexedColors.Black.Index;
        /// <summary>
        ///     上边框颜色
        /// </summary>
        public short TopBorderColor { get; set; } = IndexedColors.Black.Index;
        /// <summary>
        ///     下边框颜色
        /// </summary>
        public short BottomBorderColor { get; set; } = IndexedColors.Black.Index;
        /// <summary>
        ///     背景色填充方式
        /// </summary>
        public FillPattern FillPattern { get; set; } = FillPattern.NoFill;
        /// <summary>
        ///     背景色
        /// </summary>
        public short FillBackgroundColor { get; set; } = IndexedColors.White.Index;
        /// <summary>
        ///     背景色
        /// </summary>
        public short FillForegroundColor { get; set; } = IndexedColors.White.Index;
    }
}
