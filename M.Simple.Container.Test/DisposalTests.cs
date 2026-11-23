using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Container.Test;

[TestClass]
public class DisposalTests
{
    [TestMethod]
    public void DependenciesAreDisposedInReverseOrderAndScopesAreIndependent()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddScoped<Dependency>();
        services.AddScoped<Parent>();
        using var root = services.BuildSimpleServiceProvider();
        var first = root.CreateScope();
        using var second = root.CreateScope();
        first.ServiceProvider.GetRequiredService<Parent>();
        second.ServiceProvider.GetRequiredService<Parent>();
        first.Dispose();
        first.Dispose();
        CollectionAssert.AreEqual(new[] { "parent", "dependency" }, log);
        second.Dispose();
        CollectionAssert.AreEqual(new[] { "parent", "dependency", "parent", "dependency" }, log);
    }

    [TestMethod]
    public void CallerOwnsInstanceRegistrations()
    {
        var value = new Counted();
        var services = new ServiceCollection();
        services.AddSingleton(value);
        using (var root = services.BuildSimpleServiceProvider()) Assert.AreSame(value, root.GetRequiredService<Counted>());
        Assert.AreEqual(0, value.Disposals);
    }

    [TestMethod]
    public void FactoryAliasesOfSameInstanceDisposeOncePerScope()
    {
        var services = new ServiceCollection();
        services.AddScoped<Counted>();
        services.AddScoped<IDisposable>(p => p.GetRequiredService<Counted>());
        using var root = services.BuildSimpleServiceProvider();
        Counted value;
        using (var scope = root.CreateScope())
        {
            value = scope.ServiceProvider.GetRequiredService<Counted>();
            Assert.AreSame(value, scope.ServiceProvider.GetRequiredService<IDisposable>());
        }
        Assert.AreEqual(1, value.Disposals);
    }

    [TestMethod]
    public async Task AsyncDisposalIsAwaitedAndIdempotent()
    {
        var services = new ServiceCollection();
        services.AddScoped<AsyncCounted>();
        await using var root = services.BuildSimpleServiceProvider();
        var scope = root.CreateAsyncScope();
        var value = scope.ServiceProvider.GetRequiredService<AsyncCounted>();
        await scope.DisposeAsync();
        await scope.DisposeAsync();
        Assert.AreEqual(1, value.Disposals);
    }

    [TestMethod]
    public void SyncDisposalReportsAsyncOnlyServiceAndContinuesCleanup()
    {
        var services = new ServiceCollection();
        services.AddTransient<Counted>();
        services.AddTransient<AsyncCounted>();
        using var root = services.BuildSimpleServiceProvider();
        var scope = root.CreateScope();
        var value = scope.ServiceProvider.GetRequiredService<Counted>();
        scope.ServiceProvider.GetRequiredService<AsyncCounted>();
        StringAssert.Contains(Assert.ThrowsException<AggregateException>(() => scope.Dispose()).ToString(), "DisposeAsync");
        Assert.AreEqual(1, value.Disposals);
    }

    [TestMethod]
    public void DisposedScopeAndProviderRejectResolution()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Counted>();
        var root = services.BuildSimpleServiceProvider();
        var scope = root.CreateScope();
        scope.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => scope.ServiceProvider.GetService<Counted>());
        var liveScope = root.CreateScope();
        root.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => root.GetService<Counted>());
        Assert.ThrowsException<ObjectDisposedException>(() => liveScope.ServiceProvider.GetService<Counted>());
        Assert.ThrowsException<ObjectDisposedException>(() => root.CreateScope());
        liveScope.Dispose();
    }

    [TestMethod]
    public void FailedDisposalDoesNotSkipOtherServices()
    {
        var services = new ServiceCollection();
        services.AddTransient<Counted>();
        services.AddTransient<ThrowsOnDispose>();
        var root = services.BuildSimpleServiceProvider();
        var value = root.GetRequiredService<Counted>();
        root.GetRequiredService<ThrowsOnDispose>();
        Assert.ThrowsException<AggregateException>(() => root.Dispose());
        Assert.AreEqual(1, value.Disposals);
    }

    [TestMethod]
    public async Task ResolutionAndDisposalAreSerialized()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var services = new ServiceCollection();
        services.AddScoped(_ => { entered.SetResult(); release.Wait(TimeSpan.FromSeconds(10)); return new Counted(); });
        using var root = services.BuildSimpleServiceProvider();
        var scope = root.CreateScope();
        var resolve = Task.Run(() => scope.ServiceProvider.GetRequiredService<Counted>());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var dispose = Task.Run(() => scope.Dispose());
        release.Set();
        var value = await resolve;
        await dispose.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual(1, value.Disposals);
    }

    [TestMethod]
    public void FactoryDisposingItsOwnProviderDoesNotLeakNewInstance()
    {
        var value = new Counted();
        var services = new ServiceCollection();
        services.AddTransient<Counted>(p => { ((IDisposable)p).Dispose(); return value; });
        using var root = services.BuildSimpleServiceProvider();
        Assert.ThrowsException<ObjectDisposedException>(() => root.GetRequiredService<Counted>());
        Assert.AreEqual(1, value.Disposals);
    }

    public class Counted : IDisposable
    {
        public int Disposals;
        public void Dispose() => Disposals++;
    }
    public class AsyncCounted : IAsyncDisposable
    {
        public int Disposals;
        public async ValueTask DisposeAsync() { await Task.Yield(); Disposals++; }
    }
    public class ThrowsOnDispose : IDisposable
    {
        public void Dispose() => throw new InvalidOperationException("dispose failed");
    }
    public class Dependency(List<string> log) : IDisposable
    {
        public void Dispose() => log.Add("dependency");
    }
    public class Parent(Dependency dependency, List<string> log) : IDisposable
    {
        public Dependency Dependency { get; } = dependency;
        public void Dispose() => log.Add("parent");
    }
}
