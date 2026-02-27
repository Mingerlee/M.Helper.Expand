using System.Net.Mail;
using System.Text;

namespace M.Simple.Expand.Services
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
            SmtpClient smtpClient = GetSmtpClient();
            try
            {
                smtpClient.Send(ConvertMailMessage(Mail));
            }
            catch (Exception ex)
            {
                return new MailResult(ex.Message);
            }
            smtpClient.Dispose();
            smtpClient = null;
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
            SmtpClient smtpClient = GetSmtpClient();
            try
            {
                await smtpClient.SendMailAsync(ConvertMailMessage(Mail));
            }
            catch (Exception e)
            {
                return new MailResult($"Sending failure,error {e.Message}");
            }
            smtpClient.Dispose();
            smtpClient = null;
            return new MailResult();
        }

        private SmtpClient GetSmtpClient()
        {
            SmtpClient smtpClient = new SmtpClient();
            switch (Settings.DeliveryOption)
            {
                case MailDeliveryOptions.UseNetworkSmtp:
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.Host = Settings.StmpHostServer;
                    smtpClient.Port = Settings.PortNumber;
                    if (Settings.RequireAuth)
                    {
                        smtpClient.UseDefaultCredentials = false;
                        smtpClient.Credentials = Settings.GetCredential;
                    }

                    smtpClient.EnableSsl = Settings.EnableSSL;
                    break;
                case MailDeliveryOptions.UseIISPickUpDirectory:
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.PickupDirectoryFromIis;
                    break;
                case MailDeliveryOptions.SpecifiedPickUpDirectory:
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                    smtpClient.PickupDirectoryLocation = Settings.SpecifiedPickUpDirectoryPath;
                    break;
                default:
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.Host = Settings.StmpHostServer;
                    smtpClient.Port = Settings.PortNumber;
                    if (Settings.RequireAuth)
                    {
                        smtpClient.Credentials = Settings.GetCredential;
                    }

                    smtpClient.EnableSsl = Settings.EnableSSL;
                    break;
            }

            return smtpClient;
        }
        private MailMessage ConvertMailMessage(MailContent model)
        {
            if (!string.IsNullOrEmpty(model.ToAddresses))
            {
                MailMessage mms = new()
                {
                    From = new MailAddress(Settings.FromAddress),//必须要和用户名对应的邮件地址
                    Subject = model.Subject,
                    SubjectEncoding = Encoding.UTF8
                };
                if (!model.IsHTML)
                {
                    mms.IsBodyHtml = model.IsHTML;
                    mms.Body = model.Body;
                    mms.BodyEncoding = Encoding.UTF8;
                }
                else
                {
                    //var TextView = AlternateView.CreateAlternateViewFromString(model.Body.GetPlainTextFromHTML(), Encoding.UTF8, "text/plain");
                    var TextView = AlternateView.CreateAlternateViewFromString(model.Body, Encoding.UTF8, "text/plain");
                    var HTMLview = AlternateView.CreateAlternateViewFromString(model.Body, Encoding.UTF8, "text/html");
                    mms.AlternateViews.Add(TextView);
                    mms.AlternateViews.Add(HTMLview);
                }

                //Add to Addresses
                if (!string.IsNullOrEmpty(model.ToAddresses))
                {
                    var addresses = model.ToAddresses.GetEMailAddressArray();
                    foreach (var a in addresses)
                    {
                        mms.To.Add(new MailAddress(a));
                    }
                }
                //Add CC Addresses
                if (!string.IsNullOrEmpty(model.CcAddresses))
                {
                    var addresses = model.ToAddresses.GetEMailAddressArray();
                    foreach (var a in addresses)
                    {
                        if (!string.IsNullOrEmpty(a))
                            mms.CC.Add(new MailAddress(a));
                    }
                }
                //Add BCC Addresses
                if (!string.IsNullOrEmpty(model.BccAddresses))
                {
                    var addresses = model.ToAddresses.GetEMailAddressArray();
                    foreach (var a in addresses)
                    {
                        if (!string.IsNullOrEmpty(a))
                            mms.Bcc.Add(new MailAddress(a));
                    }
                }
                //Add ReplyTo Addresses
                if (!string.IsNullOrEmpty(model.ReplyToAddresses))
                {
                    var addresses = model.ToAddresses.GetEMailAddressArray();
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
            return null;
        }
    }
}
