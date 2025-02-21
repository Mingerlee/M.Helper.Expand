namespace M.Simple.Expand.Models
{
    /// <summary>
    /// <para>M.Simple.Expand扩展</para>
    /// <para>实体对比结果模型</para>
    /// </summary>
    public class ComparesResults
    {
        /// <summary>
        /// 字段名称
        /// </summary>
        public string? FiledName { get; set; }
        /// <summary>
        /// 修改后的值
        /// </summary>
        public string? FiledNewValue { get; set; }
        /// <summary>
        /// 修改前的值
        /// </summary>
        public string? FiledOldValue { get; set; }
        /// <summary>
        /// 是否修改
        /// </summary>
        public bool? IsModify { get; set; } = false;
        /// <summary>
        /// 是否新增
        /// </summary>
        public bool? IsAdded { get; set; } = false;
        /// <summary>
        /// 是否删除
        /// </summary>
        public bool? IsDelete { get; set; } = false;
    }
}
