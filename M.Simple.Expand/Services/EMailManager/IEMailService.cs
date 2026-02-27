using System.Net.Mail;

namespace M.Simple.Expand.Services
{
    /// <summary>
    ///     <para>M.Simple.Services服务类</para>
    ///     <para>邮件服务接口</para>
    /// </summary>
    public interface IEMailService
    {
        /// <summary>
        ///     <para>M.Simple.Services服务类</para>
        ///     <para>发送邮件</para>
        /// </summary>
        /// <param name="Mail"></param>
        /// <returns></returns>
        MailResult SendMail(MailContent Mail);
        /// <summary>
        ///     <para>M.Simple.Services服务类</para>
        ///     <para>发送邮件（异步）</para>
        /// </summary>
        /// <param name="Mail"></param>
        /// <returns></returns>
        Task<MailResult> SendMailAsync(MailContent Mail);
        /// <summary>
        ///     <para>M.Simple.Services服务类</para>
        ///     <para>获取邮件服务客户端</para>
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        SmtpClient GetSmtpClient(MailSettings settings);
    }
}
