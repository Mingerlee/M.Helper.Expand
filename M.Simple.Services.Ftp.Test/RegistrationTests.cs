using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Services.Ftp.Test;

[TestClass]
public sealed class RegistrationTests
{
    [TestMethod]
    public void ConfigureActionCreatesIndependentDirectoryState()
    {
        var services = new ServiceCollection();
        services.AddSimpleFtp(settings => { settings.FtpServerIP = "localhost"; settings.FtpRemotePath = "base"; });
        using var provider = services.BuildServiceProvider();
        var first = (FtpClientService)provider.GetRequiredService<IFtpClientService>();
        var second = (FtpClientService)provider.GetRequiredService<IFtpClientService>();
        first.GotoFtpDirectory("child", false);
        Assert.AreEqual("base/child", first.Settings.FtpRemotePath);
        Assert.AreEqual("base", second.Settings.FtpRemotePath);
    }
}
