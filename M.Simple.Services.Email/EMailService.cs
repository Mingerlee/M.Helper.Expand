using System.Net.Mail;
using System.Text;

namespace M.Simple.Services.Email
{
    /// <summary>
    ///     <para>M.Simple.Services服务类</para>
    ///     <para>邮件服务</para>
    /// </summary>
    public class EMailService : IEMailService
    {
        /// <summary>
        ///     邮件服务配置
        /// </summary>
        public MailSettings Settings { get; internal set; }
        /// <summary>
        ///     构造
        /// </summary>
        /// <param name="setting"></param>
        public EMailService(MailSettings setting)
        {
            Settings = setting;
        }
        /// <summary>
        ///     获取客户端
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public SmtpClient GetSmtpClient(MailSettings settings)
        {
            SmtpClient smtpClient = new SmtpClient();
            try
            {
                switch (settings.DeliveryOption)
                {
                    case MailDeliveryOptions.UseNetworkSmtp:
                        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtpClient.Host = settings.StmpHostServer;
                        smtpClient.Port = settings.PortNumber;
                        if (settings.RequireAuth)
                        {
                            smtpClient.UseDefaultCredentials = false;
                            smtpClient.Credentials = settings.GetCredential;
                        }

                        smtpClient.EnableSsl = settings.EnableSSL;
                        break;
                    case MailDeliveryOptions.UseIISPickUpDirectory:
                        smtpClient.DeliveryMethod = SmtpDeliveryMethod.PickupDirectoryFromIis;
                        break;
                    case MailDeliveryOptions.SpecifiedPickUpDirectory:
                        smtpClient.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                        smtpClient.PickupDirectoryLocation = settings.SpecifiedPickUpDirectoryPath;
                        break;
                    default:
                        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtpClient.Host = settings.StmpHostServer;
                        smtpClient.Port = settings.PortNumber;
                        if (settings.RequireAuth)
                        {
                            smtpClient.Credentials = settings.GetCredential;
                        }

                        smtpClient.EnableSsl = settings.EnableSSL;
                        break;
                }

                return smtpClient;
            }
            catch
            {
                smtpClient.Dispose();
                throw;
            }
        }
        /// <summary>
        ///     发送邮件
        /// </summary>
        /// <param name="Mail"></param>
        /// <returns></returns>
        public MailResult SendMail(MailContent Mail)
        {
            if (Settings == null)
            {
                return new MailResult("Settings not initialised. Please provide mail settings before send.");
            }
            try
            {
                // 消息拥有附件和正文视图，客户端及消息在成功和失败路径都必须释放。
                using SmtpClient smtpClient = GetSmtpClient();
                using MailMessage message = ConvertMailMessage(Mail);
                smtpClient.Send(message);
            }
            catch (Exception ex)
            {
                return new MailResult(ex.Message);
            }
            return new MailResult();
        }
        /// <summary>
        ///     发送邮件
        /// </summary>
        /// <param name="Mail"></param>
        /// <returns></returns>
        public async Task<MailResult> SendMailAsync(MailContent Mail)
        {
            if (Settings == null)
            {
                return new MailResult("Settings not initialised. Please provide mail settings before send.");
            }
            try
            {
                // 消息拥有附件和正文视图，客户端及消息在成功和失败路径都必须释放。
                using SmtpClient smtpClient = GetSmtpClient();
                using MailMessage message = ConvertMailMessage(Mail);
                await smtpClient.SendMailAsync(message);
            }
            catch (Exception e)
            {
                return new MailResult($"Sending failure,error {e.Message}");
            }
            return new MailResult();
        }

        private static string[] ParseAddresses(string addresses) => addresses.Replace(',', ';').Split(';');

        private SmtpClient GetSmtpClient()
        {
            return GetSmtpClient(Settings);
        }
        private MailMessage ConvertMailMessage(MailContent model)
        {
            if (!string.IsNullOrEmpty(model.ToAddresses))
            {
                MailMessage mms = new();
                try
                {
                    mms.From = new MailAddress(Settings.FromAddress);
                    mms.Subject = model.Subject;
                    mms.SubjectEncoding = Encoding.UTF8;
                    if (!model.IsHTML)
                    {
                        mms.IsBodyHtml = model.IsHTML;
                        mms.Body = model.Body;
                        mms.BodyEncoding = Encoding.UTF8;
                    }
                    else
                    {
                        //var TextView = AlternateView.CreateAlternateViewFromString(model.Body.GetPlainTextFromHTML(), Encoding.UTF8, "text/plain");
                        mms.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(model.Body, Encoding.UTF8, "text/plain"));
                        mms.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(model.Body, Encoding.UTF8, "text/html"));
                    }

                    //Add to Addresses
                    if (!string.IsNullOrEmpty(model.ToAddresses))
                    {
                        var addresses = ParseAddresses(model.ToAddresses);
                        foreach (var a in addresses)
                        {
                            mms.To.Add(new MailAddress(a));
                        }
                    }
                    //Add CC Addresses
                    if (!string.IsNullOrEmpty(model.CcAddresses))
                    {
                        // 抄送只读取抄送地址，避免误用主收件人。
                        var addresses = ParseAddresses(model.CcAddresses);
                        foreach (var a in addresses)
                        {
                            if (!string.IsNullOrEmpty(a))
                                mms.CC.Add(new MailAddress(a));
                        }
                    }
                    //Add BCC Addresses
                    if (!string.IsNullOrEmpty(model.BccAddresses))
                    {
                        // 密送使用独立地址列表。
                        var addresses = ParseAddresses(model.BccAddresses);
                        foreach (var a in addresses)
                        {
                            if (!string.IsNullOrEmpty(a))
                                mms.Bcc.Add(new MailAddress(a));
                        }
                    }
                    //Add ReplyTo Addresses
                    if (!string.IsNullOrEmpty(model.ReplyToAddresses))
                    {
                        // 回复地址与收件人分别设置。
                        var addresses = ParseAddresses(model.ReplyToAddresses);
                        foreach (var a in addresses)
                        {
                            if (!string.IsNullOrEmpty(a))
                                mms.ReplyToList.Add(new MailAddress(a));
                        }
                    }
                    //Add Attachments
                    if (model.AttachmentPaths != null)
                    {
                        foreach (var a in model.AttachmentPaths)
                        {
                            mms.Attachments.Add(new Attachment(a.Trim()));
                        }
                    }
                    return mms;
                }
                catch
                {
                    // 构建后续附件失败时，释放此前已打开的附件流。
                    mms.Dispose();
                    throw;
                }
            }
            return null;
        }
    }
}
