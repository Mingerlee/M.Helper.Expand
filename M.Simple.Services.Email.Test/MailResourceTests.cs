using M.Simple.Services.Email;

namespace M.Simple.Services.Email.Test;

[TestClass]
public class MailResourceTests
{
    [DataTestMethod]
    [DataRow(false, "success")]
    [DataRow(true, "success")]
    [DataRow(false, "send-failure")]
    [DataRow(true, "send-failure")]
    [DataRow(false, "attachment-failure")]
    [DataRow(true, "attachment-failure")]
    public async Task SendMail_ReleasesAttachmentsOnEveryExit(bool asyncMode, string scenario)
    {
        string directory = Path.Combine(Path.GetTempPath(), "mail-regression-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string attachment = Path.Combine(directory, "attachment.txt");
        File.WriteAllText(attachment, "附件内容");
        try
        {
            var service = new EMailService(new MailSettings
            {
                FromAddress = "sender@example.invalid",
                DeliveryOption = MailDeliveryOptions.SpecifiedPickUpDirectory,
                SpecifiedPickUpDirectoryPath = scenario == "send-failure" ? Path.Combine(directory, "missing") : directory
            });
            var mail = new MailContent
            {
                ToAddresses = "receiver@example.invalid",
                Subject = "资源释放回归",
                Body = "<p>正文</p>",
                IsHTML = true,
                AttachmentPaths = new List<string> { attachment }
            };
            if (scenario == "attachment-failure") mail.AttachmentPaths.Add(Path.Combine(directory, "missing.txt"));
            var result = asyncMode ? await service.SendMailAsync(mail) : service.SendMail(mail);
            Assert.AreEqual(scenario == "success" ? MailStatus.Success : MailStatus.Failure, result.Status);
            if (scenario == "success") Assert.AreEqual(1, Directory.GetFiles(directory, "*.eml").Length);
            // Windows上泄漏附件流会阻止独占打开和删除。
            using (var exclusive = new FileStream(attachment, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Assert.IsTrue(exclusive.Length > 0);
            File.Delete(attachment);
            Assert.IsFalse(File.Exists(attachment));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SendMail_InvalidClientSettingsReturnsFailure(bool asyncMode)
    {
        var service = new EMailService(new MailSettings
        {
            StmpHostServer = "localhost",
            PortNumber = 0,
            DeliveryOption = MailDeliveryOptions.UseNetworkSmtp
        });
        var result = asyncMode ? await service.SendMailAsync(new MailContent()) : service.SendMail(new MailContent());
        Assert.AreEqual(MailStatus.Failure, result.Status);
    }
}
