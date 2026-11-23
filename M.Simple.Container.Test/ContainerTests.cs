using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Container.Test;

[TestClass]
public class ContainerTests
{
    [TestMethod]
    public void LifetimesAndProviderIdentity()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Singleton>();
        services.AddScoped<Scoped>();
        services.AddTransient<Transient>();
        using var root = services.BuildSimpleServiceProvider();
        using var a = root.CreateScope();
        using var b = root.CreateScope();
        Assert.AreSame(a.ServiceProvider.GetRequiredService<Singleton>(), b.ServiceProvider.GetRequiredService<Singleton>());
        Assert.AreSame(a.ServiceProvider.GetRequiredService<Scoped>(), a.ServiceProvider.GetRequiredService<Scoped>());
        Assert.AreNotSame(a.ServiceProvider.GetRequiredService<Scoped>(), b.ServiceProvider.GetRequiredService<Scoped>());
        Assert.AreNotSame(a.ServiceProvider.GetRequiredService<Transient>(), a.ServiceProvider.GetRequiredService<Transient>());
        Assert.AreSame(root, root.GetRequiredService<IServiceProvider>());
        Assert.AreSame(a.ServiceProvider, a.ServiceProvider.GetRequiredService<IServiceProvider>());
        Assert.AreSame(root, a.ServiceProvider.GetRequiredService<IServiceScopeFactory>());
        Assert.ThrowsException<InvalidOperationException>(() => root.GetRequiredService<Scoped>());
    }

    [TestMethod]
    public void ConstructorsOptionalArgumentsAndMissingServices()
    {
        var services = new ServiceCollection();
        services.AddTransient<Optional>();
        services.AddTransient<Greedy>();
        services.AddTransient<DefaultValue>();
        services.AddSingleton<Singleton>();
        using var root = services.BuildSimpleServiceProvider();
        Assert.AreEqual(42, root.GetRequiredService<Optional>().Value);
        Assert.AreEqual(default(DateTime), root.GetRequiredService<DefaultValue>().Value);
        Assert.AreEqual(1, root.GetRequiredService<Greedy>().Selected);
        Assert.IsNull(root.GetService(typeof(Scoped)));
        Assert.AreEqual(0, root.GetServices<Scoped>().Count());
        Assert.ThrowsException<InvalidOperationException>(() => root.GetRequiredService<Scoped>());
    }

    [TestMethod]
    public void LastRegistrationWinsWhileEnumerablePreservesOrder()
    {
        var services = new ServiceCollection();
        services.AddTransient<INamed, First>();
        services.AddTransient<INamed, Second>();
        using var root = services.BuildSimpleServiceProvider();
        Assert.IsInstanceOfType(root.GetRequiredService<INamed>(), typeof(Second));
        CollectionAssert.AreEqual(new[] { typeof(First), typeof(Second) }, root.GetServices<INamed>().Select(x => x.GetType()).ToArray());
    }

    [TestMethod]
    public void OpenGenericsAndClosedRegistrationPrecedence()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(IBox<>), typeof(Box<>));
        services.AddSingleton<IBox<int>, IntBox>();
        using var root = services.BuildSimpleServiceProvider();
        Assert.IsInstanceOfType(root.GetRequiredService<IBox<string>>(), typeof(Box<string>));
        Assert.IsInstanceOfType(root.GetRequiredService<IBox<int>>(), typeof(IntBox));
        CollectionAssert.AreEqual(new[] { typeof(Box<int>), typeof(IntBox) }, root.GetServices<IBox<int>>().Select(x => x.GetType()).ToArray());
        Assert.ThrowsException<ArgumentException>(() => root.GetService(typeof(IBox<>)));
    }

    [TestMethod]
    public void ConstrainedGenericsAreSkippedInEnumerable()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(IBox<>), typeof(StructBox<>));
        using var root = services.BuildSimpleServiceProvider();
        Assert.AreEqual(0, root.GetServices<IBox<string>>().Count());
        Assert.AreEqual(1, root.GetServices<IBox<int>>().Count());
        Assert.ThrowsException<InvalidOperationException>(() => root.GetRequiredService<IBox<string>>());
    }

    [TestMethod]
    public void KeyedServicesSupportInjectionKeyAndAnyKeyEnumerable()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<INamed, First>("first");
        services.AddKeyedSingleton<INamed, Second>("second");
        services.AddTransient<KeyConsumer>();
        services.AddKeyedTransient<KeyAware>("hello");
        using var root = services.BuildSimpleServiceProvider();
        Assert.IsInstanceOfType(root.GetRequiredService<KeyConsumer>().Service, typeof(First));
        Assert.AreEqual("hello", root.GetRequiredKeyedService<KeyAware>("hello").Key);
        Assert.AreEqual(2, root.GetKeyedServices<INamed>(KeyedService.AnyKey).Count());
        Assert.AreEqual(0, root.GetServices<INamed>().Count());
        Assert.IsTrue(root.IsKeyedService(typeof(INamed), "second"));
        Assert.IsNull(root.GetKeyedService(typeof(INamed), "unknown"));
        Assert.ThrowsException<InvalidOperationException>(() => root.GetKeyedService(typeof(INamed), KeyedService.AnyKey));
    }

    [TestMethod]
    public void KeyedFactoriesReceiveTheirKeyAndScope()
    {
        var services = new ServiceCollection();
        services.AddKeyedScoped<INamed>("x", (p, key) => new NamedValue($"{key}:{ReferenceEquals(p, p.GetRequiredService<IServiceProvider>())}"));
        using var root = services.BuildSimpleServiceProvider();
        using var scope = root.CreateScope();
        var service = scope.ServiceProvider.GetRequiredKeyedService<INamed>("x");
        Assert.AreEqual("x:True", ((NamedValue)service).Name);
        Assert.AreSame(service, scope.ServiceProvider.GetRequiredKeyedService<INamed>("x"));
    }

    [TestMethod]
    public void WildcardRegistrationIsRejectedExplicitly()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<INamed, First>(KeyedService.AnyKey);
        Assert.ThrowsException<NotSupportedException>(() => services.BuildSimpleServiceProvider());
    }

    [TestMethod]
    public void BuildValidationReportsMissingDependencyAndCycle()
    {
        var missing = new ServiceCollection();
        missing.AddTransient<NeedsMissing>();
        var error = Assert.ThrowsException<AggregateException>(() => missing.BuildSimpleServiceProvider()).ToString();
        StringAssert.Contains(error, nameof(NeedsMissing));
        StringAssert.Contains(error, nameof(IMissing));
        var cycle = new ServiceCollection();
        cycle.AddTransient<CycleA>();
        cycle.AddTransient<CycleB>();
        StringAssert.Contains(Assert.ThrowsException<AggregateException>(() => cycle.BuildSimpleServiceProvider()).ToString(), "Circular dependency");
    }

    [TestMethod]
    public void CaptiveScopeDetectedDirectlyAndThroughTransientAndEnumerable()
    {
        var direct = new ServiceCollection();
        direct.AddScoped<Scoped>();
        direct.AddSingleton<NeedsScoped>();
        Assert.ThrowsException<AggregateException>(() => direct.BuildSimpleServiceProvider());
        var indirect = new ServiceCollection();
        indirect.AddScoped<Scoped>();
        indirect.AddTransient<NeedsScoped>();
        indirect.AddSingleton<NeedsIndirectScoped>();
        Assert.ThrowsException<AggregateException>(() => indirect.BuildSimpleServiceProvider());
        var enumerable = new ServiceCollection();
        enumerable.AddScoped<Scoped>();
        enumerable.AddSingleton<NeedsScopedList>();
        Assert.ThrowsException<AggregateException>(() => enumerable.BuildSimpleServiceProvider());
    }

    [TestMethod]
    public void DynamicFactoryCyclesAndCaptiveScopesAreDetectedAtRuntime()
    {
        var services = new ServiceCollection();
        services.AddTransient<FactoryLoop>(p => p.GetRequiredService<FactoryLoop>());
        services.AddScoped<Scoped>();
        services.AddSingleton(p => new NeedsScoped(p.GetRequiredService<Scoped>()));
        using var root = services.BuildSimpleServiceProvider();
        StringAssert.Contains(Assert.ThrowsException<InvalidOperationException>(() => root.GetRequiredService<FactoryLoop>()).Message, "Circular factory");
        using var scope = root.CreateScope();
        Assert.ThrowsException<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<NeedsScoped>());
    }

    [TestMethod]
    public void SingletonFactoryReceivesRootAndCapturesTransientThere()
    {
        var services = new ServiceCollection();
        services.AddTransient<Disposable>();
        services.AddSingleton(p => new HoldsDisposable(p.GetRequiredService<Disposable>(), p));
        using var root = services.BuildSimpleServiceProvider();
        HoldsDisposable value;
        using (var scope = root.CreateScope()) value = scope.ServiceProvider.GetRequiredService<HoldsDisposable>();
        Assert.AreSame(root, value.Provider);
        Assert.AreEqual(0, value.Dependency.Disposals);
        root.Dispose();
        Assert.AreEqual(1, value.Dependency.Disposals);
    }

    [TestMethod]
    public void BuildFreezesRegistrationSnapshotAndOptions()
    {
        var services = new ServiceCollection();
        services.AddScoped<Scoped>();
        SimpleContainerOptions? options = null;
        using var root = services.BuildSimpleServiceProvider(o => options = o);
        services.AddSingleton<Singleton>();
        options!.ValidateScopes = false;
        Assert.IsNull(root.GetService(typeof(Singleton)));
        Assert.ThrowsException<InvalidOperationException>(() => root.GetRequiredService<Scoped>());
    }

    [TestMethod]
    public void ValidationCanBeDisabledExplicitly()
    {
        var services = new ServiceCollection();
        services.AddScoped<Scoped>();
        services.AddTransient<NeedsMissing>();
        using var root = services.BuildSimpleServiceProvider(o => { o.ValidateOnBuild = false; o.ValidateScopes = false; });
        Assert.AreSame(root.GetRequiredService<Scoped>(), root.GetRequiredService<Scoped>());
        Assert.ThrowsException<InvalidOperationException>(() => root.GetRequiredService<NeedsMissing>());
    }

    [TestMethod]
    public void AmbiguousConstructorsAreRejected()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Singleton>();
        services.AddTransient<Transient>();
        services.AddTransient<Ambiguous>();
        StringAssert.Contains(Assert.ThrowsException<AggregateException>(() => services.BuildSimpleServiceProvider()).ToString(), "Ambiguous");
    }

    [TestMethod]
    public async Task ConcurrentSingletonAndScopedFactoriesRunOnce()
    {
        var singletonCount = 0;
        var scopedCount = 0;
        var services = new ServiceCollection();
        services.AddSingleton(_ => { Interlocked.Increment(ref singletonCount); return new Singleton(); });
        services.AddScoped(_ => { Interlocked.Increment(ref scopedCount); return new Scoped(); });
        using var root = services.BuildSimpleServiceProvider();
        using var scope = root.CreateScope();
        var values = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() =>
            (scope.ServiceProvider.GetRequiredService<Singleton>(), scope.ServiceProvider.GetRequiredService<Scoped>()))));
        Assert.AreEqual(1, singletonCount);
        Assert.AreEqual(1, scopedCount);
        Assert.IsTrue(values.All(x => ReferenceEquals(x.Item1, values[0].Item1) && ReferenceEquals(x.Item2, values[0].Item2)));
    }

    [TestMethod]
    public async Task ConcurrentIndependentScopesAndSingletonResolutionComplete()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Singleton>();
        services.AddScoped<ScopedGraph>();
        using var root = services.BuildSimpleServiceProvider();
        await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() =>
        {
            using var scope = root.CreateScope();
            Assert.AreSame(root.GetRequiredService<Singleton>(), scope.ServiceProvider.GetRequiredService<ScopedGraph>().Singleton);
        }))).WaitAsync(TimeSpan.FromSeconds(15));
    }

    [TestMethod]
    public void FailedFactoryCanBeRetried()
    {
        var attempts = 0;
        var services = new ServiceCollection();
        services.AddSingleton<Singleton>(_ => Interlocked.Increment(ref attempts) == 1 ? throw new InvalidOperationException("retry") : new Singleton());
        using var root = services.BuildSimpleServiceProvider();
        Assert.ThrowsException<InvalidOperationException>(() => root.GetRequiredService<Singleton>());
        Assert.IsNotNull(root.GetRequiredService<Singleton>());
        Assert.AreEqual(2, attempts);
    }

    [TestMethod]
    public void FactoryReturningNullIsCached()
    {
        var count = 0;
        var services = new ServiceCollection();
        services.AddSingleton<Singleton>(_ => { count++; return null!; });
        using var root = services.BuildSimpleServiceProvider();
        Assert.IsNull(root.GetService(typeof(Singleton)));
        Assert.IsNull(root.GetService(typeof(Singleton)));
        Assert.AreEqual(1, count);
    }

    public class Singleton;
    public class Scoped;
    public class Transient;
    public class Optional(int value = 42) { public int Value { get; } = value; }
    public class DefaultValue(DateTime value = default) { public DateTime Value { get; } = value; }
    public class Greedy
    {
        public int Selected { get; }
        public Greedy() { }
        public Greedy(Singleton _) { Selected = 1; }
    }
    public interface INamed;
    public class First : INamed;
    public class Second : INamed;
    public record NamedValue(string Name) : INamed;
    public interface IBox<T>;
    public class Box<T> : IBox<T>;
    public class IntBox : IBox<int>;
    public class StructBox<T> : IBox<T> where T : struct;
    public class KeyConsumer([FromKeyedServices("first")] INamed service) { public INamed Service { get; } = service; }
    public class KeyAware([ServiceKey] string key) { public string Key { get; } = key; }
    public interface IMissing;
    public record NeedsMissing(IMissing Value);
    public record CycleA(CycleB Value);
    public record CycleB(CycleA Value);
    public record NeedsScoped(Scoped Value);
    public record NeedsIndirectScoped(NeedsScoped Value);
    public record NeedsScopedList(IEnumerable<Scoped> Values);
    public class FactoryLoop;
    public record HoldsDisposable(Disposable Dependency, IServiceProvider Provider);
    public class ScopedGraph(Singleton singleton) { public Singleton Singleton { get; } = singleton; }
    public class Ambiguous
    {
        public Ambiguous(Singleton _) { }
        public Ambiguous(Transient _) { }
    }
    public class Disposable : IDisposable
    {
        public int Disposals;
        public void Dispose() => Disposals++;
    }
}
