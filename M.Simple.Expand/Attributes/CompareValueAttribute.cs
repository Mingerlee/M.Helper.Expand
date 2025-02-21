using System;

namespace M.Simple.Expand.Attributes
{
    /// <summary>
    /// <para>M.Simple.Expand扩展</para>
    /// <para>需要对比的属性</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
    public class CompareValueAttribute : Attribute
    {

    }
}
