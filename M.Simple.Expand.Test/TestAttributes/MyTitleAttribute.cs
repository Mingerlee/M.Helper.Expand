using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand.Test.TestAttributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class MyTitleAttribute(string? title = null) : Attribute
    {
        /// <summary>
        ///     称谓
        /// </summary>
        public string Title { get; set; } = title ?? "";
    }
}
