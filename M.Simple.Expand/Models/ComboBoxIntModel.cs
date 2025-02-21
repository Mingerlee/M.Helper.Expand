using System.Collections.Generic;

namespace M.Simple.Expand.Models
{
    /// <summary>
    /// <para>M.Simple.Expand扩展</para>    
    /// <para>下拉框 数值类型选项</para>    
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
    public class ComboBoxIntViewModel
    {
        /// <summary>
        ///      数值下拉框选项集
        /// </summary>
        public IEnumerable<ComboBoxIntModel> SelectOptions { get; set; }
    }
}
