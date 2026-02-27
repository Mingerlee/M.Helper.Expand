using System.ComponentModel;

namespace M.Simple.Expand.Services
{
    /// <summary>
    ///     <para>M.Simple.Services服务类</para>    
    ///     <para>邮件状态</para>
    /// </summary>
    [Description("邮件状态")]
    public enum MailStatus
    {
        /// <summary>
        ///     失败
        /// </summary>
        [Description("失败")]
        Failure = 0,
        /// <summary>
        ///     成功
        /// </summary>
        [Description("成功")]
        Success = 1,
    }
    /// <summary>
    ///     <para>M.Simple.Services服务类</para>    
    ///     <para>邮件发送选项</para>
    /// </summary>
    public enum MailDeliveryOptions
    {
        /// <summary>
        ///     使用网络Smtp
        /// </summary>
        UseNetworkSmtp,
        /// <summary>
        ///     使用IIS目录
        /// </summary>
        UseIISPickUpDirectory,
        /// <summary>
        ///     指定PickUp目录
        /// </summary>
        SpecifiedPickUpDirectory
    }
}
