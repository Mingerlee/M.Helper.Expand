using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using M.Simple.Services.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Services.DependencyInjection.Test;

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
