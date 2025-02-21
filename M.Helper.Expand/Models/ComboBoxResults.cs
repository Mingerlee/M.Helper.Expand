using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Helper.Expand
{
    /// <summary>
    ///     下拉框 数值类型选项
    /// </summary>
    public class ComboBoxIntModel
    {
        /// <summary>
        ///      值
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        ///      文本
        /// </summary>
        public string Text { get; set; }
    }
    /// <summary>
    ///     下拉框 字符串类型选项
    /// </summary>
    public class ComboBoxStringModel
    {
        /// <summary>
        ///      值
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        ///      文本
        /// </summary>
        public string Text { get; set; }
    }
    /// <summary>
    ///     数值类型下拉框显示
    /// </summary>
    public class ComboBoxIntViewModel
    {
        /// <summary>
        ///      数值下拉框选项集
        /// </summary>
        public IEnumerable<ComboBoxIntModel> SelectOptions { get; set; }
    }
    /// <summary>
    ///     字符串类型下拉框显示
    /// </summary>
    public class ComboBoxStringViewModel
    {
        /// <summary>
        ///      字符串下拉框选项集
        /// </summary>
        public IEnumerable<ComboBoxStringModel> SelectOptions { get; set; }
    }
}
