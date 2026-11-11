using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using M.Simple.Expand.Attributes;
using M.Simple.Expand.Services;
using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Expand.Test;

[TestClass]
public class ServiceConcurrencyTests
{
    [TestMethod]
    [Timeout(30000)]
    public async Task Autowired_ConcurrentColdCacheHandlesManyTypesAndProviders()
    {
        // 新类型保证每次从冷缓存开始；不同provider必须注入各自的依赖实例。
        var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("AutowiredTests" + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.Run).DefineDynamicModule("Consumers");
        var types = Enumerable.Range(0, 128).Select(i => typeof(Consumer<>).MakeGenericType(
            module.DefineType("Marker" + i, TypeAttributes.Public).CreateType())).ToArray();
        var autowired = new AutowiredService(new TestProvider(new Dependency()));
        var errors = new ConcurrentQueue<Exception>();
        using var ready = new CountdownEvent(16);
        using var start = new ManualResetEventSlim();
        var workers = Enumerable.Range(0, 16).Select(worker => Task.Factory.StartNew(() =>
        {
            var dependency = new Dependency();
            var provider = new TestProvider(dependency);
            ready.Signal();
            start.Wait();
            for (int i = 0; i < types.Length * 2; i++)
            {
                try
                {
                    var target = (Consumer)Activator.CreateInstance(types[(i + worker * 7) % types.Length]);
                    autowired.Autowired(target, provider);
                    Assert.AreSame(dependency, target.Property);
                    Assert.AreSame(dependency, target.Field);
                }
                catch (Exception error) { errors.Enqueue(error); }
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
        bool allReady = ready.Wait(TimeSpan.FromSeconds(10));
        start.Set();
        await Task.WhenAll(workers);
        Assert.IsTrue(allReady);
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors.Take(3).Select(e => e.ToString())));
    }

    [TestMethod]
    public void Autowired_CachedActionUsesCurrentScopeAndSupportsEmptyConsumer()
    {
        var services = new ServiceCollection();
        services.AddScoped<Dependency>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var autowired = new AutowiredService(provider);
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var a = new Consumer<int>();
        var b = new Consumer<int>();
        autowired.Autowired(a, first.ServiceProvider);
        autowired.Autowired(b, second.ServiceProvider);
        Assert.AreSame(first.ServiceProvider.GetRequiredService<Dependency>(), a.Property);
        Assert.AreSame(second.ServiceProvider.GetRequiredService<Dependency>(), b.Property);
        Assert.AreNotSame(a.Property, b.Property);
        autowired.Autowired(new object());
    }

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

    public class Dependency { }
    public class Consumer
    {
        [Autowired] public Dependency Property { get; set; }
        [Autowired] public Dependency Field;
    }
    public class Consumer<T> : Consumer { }
    private sealed class TestProvider(Dependency dependency) : IServiceProvider
    {
        public object GetService(Type serviceType) => serviceType == typeof(Dependency) ? dependency : null;
    }
}
