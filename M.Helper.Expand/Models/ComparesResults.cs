using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Helper.Expand
{
    /// <summary>
    ///     
    /// </summary>
    public class CompareValueAttribute : Attribute
    {

    }
    /// <summary>
    ///     需要修改的字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
    public class ModifyValueAttribute : Attribute
    {
        private string? _flidName;  //代码检查人
        //参数构造器
        /// <summary>
        ///     
        /// </summary>
        /// <param name="_flidName">字段名称</param>
        public ModifyValueAttribute(string? _flidName = null)
        {
            FlidName = _flidName ?? "";
        }
        /// <summary>
        ///     字段名称
        /// </summary>
        public string FlidName { get; set; }
    }
    
    public class Results
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
