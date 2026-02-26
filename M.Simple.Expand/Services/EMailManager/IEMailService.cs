using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand.Services
{
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>邮件服务接口</para>
    /// </summary>
    public interface IEMailService
    {
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>发送邮件</para>
        /// </summary>
        /// <param name="Mail"></param>
        /// <returns></returns>
        MailResult SendMail(MailContent Mail);
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>发送邮件（异步）</para>
        /// </summary>
        /// <param name="Mail"></param>
        /// <returns></returns>
        Task<MailResult> SendMailAsync(MailContent Mail);
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>获取邮件服务客户端</para>
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        SmtpClient GetSmtpClient(MailSettings settings);
    }
}
