using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;

using M.Simple.Services.Ftp;
using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Services.Ftp.Test;

[TestClass]
public class ServiceConcurrencyTests
{
    [TestMethod]
    [Timeout(20000)]
    public async Task Ftp_IndependentResolutionsCannotChangeEachOthersDirectory()
    {
        var template = Settings("localhost", "base", "test");
        var services = new ServiceCollection();
        services.AddFtpClientSetup(_ => template);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var first = scope.ServiceProvider.GetRequiredService<IFtpClientService>();
        var second = scope.ServiceProvider.GetRequiredService<IFtpClientService>();
        var firstReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var a = Task.Run(async () =>
        {
            first.GotoFtpDirectory("first", true);
            firstReady.SetResult(true);
            await secondReady.Task;
            return Request(first).RequestUri.AbsolutePath;
        });
        var b = Task.Run(async () =>
        {
            await firstReady.Task;
            second.GotoFtpDirectory("second", true);
            secondReady.SetResult(true);
            return Request(second).RequestUri.AbsolutePath;
        });
        await Task.WhenAll(a, b);
        Assert.AreEqual("/first/report.txt", a.Result);
        Assert.AreEqual("/second/report.txt", b.Result);
        Assert.AreNotSame(first, second);
        Assert.AreEqual("base", template.FtpRemotePath);
    }

    [TestMethod]
    public void Ftp_ConstructionResetAndSettingsReadUseIndependentSnapshots()
    {
        var input = Settings("localhost", "base", "original");
        var first = new FtpClientService(input);
        var second = new FtpClientService(input);
        input.FtpRemotePath = "external";
        input.FtpUserID = "changed";
        first.GotoFtpDirectory("child", false);
        Assert.AreEqual("/base/child/report.txt", Request(first).RequestUri.AbsolutePath);
        Assert.AreEqual("/base/report.txt", Request(second).RequestUri.AbsolutePath);
        Assert.AreEqual("original", ((NetworkCredential)Request(first).Credentials).UserName);
        var snapshot = first.Settings;
        snapshot.FtpRemotePath = "not-applied";
        Assert.AreEqual("/base/child/report.txt", Request(first).RequestUri.AbsolutePath);
        var replacement = Settings("localhost", "reset", "new");
        first.ResetFtpClientSettings(replacement);
        replacement.FtpRemotePath = "later-change";
        Assert.AreEqual("/reset/report.txt", Request(first).RequestUri.AbsolutePath);
        Assert.AreEqual("/base/report.txt", Request(second).RequestUri.AbsolutePath);
    }

    [TestMethod]
    public void Ftp_ConcurrentResetNeverMixesEndpointAndCredentials()
    {
        var first = Settings("first.example.invalid", "first", "first");
        var second = Settings("second.example.invalid", "second", "second");
        var service = new FtpClientService(first);
        var failures = new ConcurrentQueue<string>();
        Parallel.For(0, 5000, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i =>
        {
            service.ResetFtpClientSettings(i % 2 == 0 ? first : second);
            var request = Request(service);
            var user = ((NetworkCredential)request.Credentials).UserName;
            if (request.RequestUri.Host != user + ".example.invalid" || request.RequestUri.AbsolutePath != "/" + user + "/report.txt")
                failures.Enqueue("请求混用了两份配置。");
        });
        Assert.AreEqual(0, failures.Count);
    }

    // 只构造请求，核对实际URL及认证快照，不连接外部FTP。
    private static FtpWebRequest Request(IFtpClientService service)
    {
        return (FtpWebRequest)typeof(FtpClientService).GetMethod("CreateFtpWebRequest", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(service, new object[] { "report.txt" });
    }

    private static FtpClientSettings Settings(string host, string path, string user)
    {
        return new FtpClientSettings
        {
            FtpServerIP = host,
            FtpRemotePath = path,
            FtpUserID = user,
            FtpPassword = "test-only"
        };
    }

}
