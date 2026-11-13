using System.Net.Mail;
using System.Reflection;
using M.Simple.Services.Email;

namespace M.Simple.Services.Email.Test;

[TestClass]
public class EMailAddressTests
{
    [DataTestMethod]
    [DataRow(",")]
    [DataRow(";")]
    public void ConvertMailMessage_UsesEachAddressListIndependently(string separator)
    {
        using var message = Convert(new MailContent
        {
            ToAddresses = $"to1@example.invalid{separator}to2@example.invalid",
            CcAddresses = $"cc1@example.invalid{separator}cc2@example.invalid",
            BccAddresses = $"bcc1@example.invalid{separator}bcc2@example.invalid",
            ReplyToAddresses = $"reply1@example.invalid{separator}reply2@example.invalid"
        });

        AssertAddresses(message.To, "to1@example.invalid", "to2@example.invalid");
        AssertAddresses(message.CC, "cc1@example.invalid", "cc2@example.invalid");
        AssertAddresses(message.Bcc, "bcc1@example.invalid", "bcc2@example.invalid");
        AssertAddresses(message.ReplyToList, "reply1@example.invalid", "reply2@example.invalid");
    }

    [DataTestMethod]
    [DataRow("cc")]
    [DataRow("bcc")]
    [DataRow("reply")]
    public void ConvertMailMessage_OptionalAddressesDoNotPopulateOtherLists(string selected)
    {
        using var message = Convert(new MailContent
        {
            ToAddresses = "to@example.invalid",
            CcAddresses = selected == "cc" ? "cc@example.invalid" : null,
            BccAddresses = selected == "bcc" ? "bcc@example.invalid" : null,
            ReplyToAddresses = selected == "reply" ? "reply@example.invalid" : null
        });

        AssertAddresses(message.To, "to@example.invalid");
        AssertAddresses(message.CC, selected == "cc" ? new[] { "cc@example.invalid" } : Array.Empty<string>());
        AssertAddresses(message.Bcc, selected == "bcc" ? new[] { "bcc@example.invalid" } : Array.Empty<string>());
        AssertAddresses(message.ReplyToList, selected == "reply" ? new[] { "reply@example.invalid" } : Array.Empty<string>());
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void ConvertMailMessage_EmptyOptionalAddressesRemainEmpty(string value)
    {
        using var message = Convert(new MailContent
        {
            ToAddresses = "to@example.invalid",
            CcAddresses = value,
            BccAddresses = value,
            ReplyToAddresses = value
        });

        AssertAddresses(message.To, "to@example.invalid");
        Assert.AreEqual(0, message.CC.Count);
        Assert.AreEqual(0, message.Bcc.Count);
        Assert.AreEqual(0, message.ReplyToList.Count);
    }

    // 只检查内存中的邮件模型，不调用发送接口或建立SMTP连接。
    private static MailMessage Convert(MailContent content)
    {
        var service = new EMailService(new MailSettings
        {
            FromAddress = "from@example.invalid"
        });
        var method = typeof(EMailService).GetMethod("ConvertMailMessage", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return (MailMessage)method.Invoke(service, new object[] { content });
    }

    private static void AssertAddresses(MailAddressCollection actual, params string[] expected)
    {
        CollectionAssert.AreEqual(expected, actual.Select(address => address.Address).ToArray());
    }
}
