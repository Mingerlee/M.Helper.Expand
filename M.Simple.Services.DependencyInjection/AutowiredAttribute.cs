using System;

namespace M.Simple.Services.DependencyInjection
{
    /// <summary>
    /// <para>M.Simple.Expand扩展</para>
    /// <para>属性注入--自动注入特性</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AutowiredAttribute : Attribute
    {
    }
}
