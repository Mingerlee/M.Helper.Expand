using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand.Attributes
{
    /// <summary>
    /// <para>M.Simple.Expand扩展</para>
    /// <para>需要修改的属性</para>
    /// </summary>
    /// <remarks>
    ///     
    /// </remarks>
    /// <param name="flidName">属性名称</param>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
    public class ModifyValueAttribute(string? flidName = null) : Attribute
    {
        /// <summary>
        ///     字段名称
        /// </summary>
        public string FlidName { get; set; } = flidName ?? "";
    }
}
