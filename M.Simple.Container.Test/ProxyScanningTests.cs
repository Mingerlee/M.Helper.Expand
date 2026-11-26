using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Container.Test;

[TestClass]
public sealed class ProxyScanningTests
{
    public interface IMarker { }
    public interface IFirst { string Run(string value); }
    public interface ISecond { Guid Id(); }
    public sealed class State { public int Disposed; }
    public sealed class Service(State state) : IMarker, IFirst, ISecond, IDisposable
    {
        private readonly Guid _id = Guid.NewGuid();
        public string Run(string value) => value;
        public Guid Id() => _id;
        public void Dispose() => state.Disposed++;
    }
    public sealed class Other : IFirst { public string Run(string value) => value; }

    private static AssemblyRegistrationBuilder Scan(IServiceCollection services)
        => services.Scan(typeof(Service).Assembly).AssignableTo<IMarker>().ExcludeInterface(typeof(IMarker)).AsImplementedInterfaces();

    [DataTestMethod]
    [DataRow(false, ServiceLifetime.Scoped)]
    [DataRow(true, ServiceLifetime.Scoped)]
    [DataRow(false, ServiceLifetime.Singleton)]
    [DataRow(true, ServiceLifetime.Singleton)]
    [DataRow(false, ServiceLifetime.Transient)]
    [DataRow(true, ServiceLifetime.Transient)]
    public void ProxiesRespectLifetimesAndShareTargets(bool simple, ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        var state = new State();
        services.AddSingleton(state);
        var scan = Scan(services).AsSelf();
        switch (lifetime)
        {
            case ServiceLifetime.Singleton: scan.ProxySingleton(CreateProxy); break;
            case ServiceLifetime.Transient: scan.ProxyTransient(CreateProxy); break;
            default: scan.ProxyScoped(CreateProxy); break;
        }
        using (var provider = simple ? (ServiceProviderBase) new SimpleProvider(services) : new StandardProvider(services))
        {
            using var scope = provider.Provider.CreateScope();
            var sp = scope.ServiceProvider;
            Assert.IsFalse(sp.GetServices<IMarker>().Any());
            var first = sp.GetRequiredService<IFirst>();
            Assert.AreEqual("hello:before:after", first.Run("hello"));
            Assert.IsFalse(first is Service);
            var second = sp.GetRequiredService<ISecond>();
            var target = sp.GetRequiredService<Service>();
            using var other = provider.Provider.CreateScope();
            if (lifetime == ServiceLifetime.Transient)
            {
                Assert.AreNotSame(first, sp.GetRequiredService<IFirst>());
                Assert.AreNotEqual(second.Id(), target.Id());
            }
            else
            {
                Assert.AreSame(first, sp.GetRequiredService<IFirst>());
                Assert.AreEqual(second.Id(), target.Id());
                if (lifetime == ServiceLifetime.Singleton)
                    Assert.AreSame(first, other.ServiceProvider.GetRequiredService<IFirst>());
                else Assert.AreNotSame(first, other.ServiceProvider.GetRequiredService<IFirst>());
            }
        }
        Assert.IsTrue(state.Disposed > 0);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ReplacesOrdinaryMappingWithoutDuplicatesAndPreservesOtherImplementations(bool simple)
    {
        var services = new ServiceCollection();
        var state = new State();
        services.AddSingleton(state);
        services.AddScoped<IFirst, Other>();
        Scan(services).Scoped();
        Scan(services).ProxyScoped(CreateProxy);
        var count = services.Count;
        Scan(services).ProxyScoped(CreateProxy);
        Assert.AreEqual(count, services.Count);
        using (var provider = simple ? (ServiceProviderBase)new SimpleProvider(services) : new StandardProvider(services))
        using (var scope = provider.Provider.CreateScope())
        {
            var all = scope.ServiceProvider.GetServices<IFirst>().ToArray();
            Assert.AreEqual(2, all.Length);
            Assert.IsInstanceOfType(all[0], typeof(Other));
            Assert.AreEqual("hello:before:after", all[1].Run("hello"));
            Assert.AreEqual(scope.ServiceProvider.GetRequiredService<Service>().Id(), scope.ServiceProvider.GetRequiredService<ISecond>().Id());
        }
        Assert.AreEqual(1, state.Disposed);
    }

    public interface IInvalid : IDisposable { void Run(ref int value); }
    public sealed class ZInvalid : IInvalid { public void Run(ref int value) { } public void Dispose() { } }
    public interface IGeneric<T> { T Echo(T value); }
    public sealed class Generic<T> : IGeneric<T> { public T Echo(T value) => value; }

    [TestMethod]
    public void InvalidAndOpenGenericProxyScansDoNotPartiallyRegister()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new State());
        var original = services.ToArray();
        Assert.ThrowsException<NotSupportedException>(() => services.Scan(typeof(Service).Assembly)
            .Where(t => t == typeof(Service) || t == typeof(ZInvalid)).ProxyScoped(CreateProxy));
        CollectionAssert.AreEqual(original, services.ToArray());
        Assert.ThrowsException<NotSupportedException>(() => services.Scan(typeof(Service).Assembly)
            .Where(t => t == typeof(Generic<>)).ProxyScoped(CreateProxy));
        CollectionAssert.AreEqual(original, services.ToArray());
    }

    public class RecordingProxy : DispatchProxy
    {
        public object Target { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IFirst.Run)) args![0] += ":before";
            var result = method.Invoke(Target, args);
            return method.Name == nameof(IFirst.Run) ? result + ":after" : result;
        }
    }

    private static object CreateProxy(Type contract, object target, IServiceProvider provider)
    {
        _ = provider.GetRequiredService<State>();
        var proxy = DispatchProxy.Create(contract, typeof(RecordingProxy));
        ((RecordingProxy)proxy).Target = target;
        return proxy;
    }

    [TestMethod]
    public void SpecifiedInterfaceOnlyIsProxied()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new State());
        services.Scan(typeof(Service).Assembly).AssignableTo<IMarker>().As<IFirst>()
            .WithProxy(CreateProxy).Scoped();
        using var provider = services.BuildSimpleServiceProvider();
        using var scope = provider.CreateScope();
        Assert.IsFalse(scope.ServiceProvider.GetServices<IMarker>().Any());
        Assert.IsFalse(scope.ServiceProvider.GetServices<ISecond>().Any());
        Assert.AreEqual("hello:before:after", scope.ServiceProvider.GetRequiredService<IFirst>().Run("hello"));
    }

    [TestMethod]
    public void LifetimeMismatchIsRejectedWithoutChangingRegistrations()
    {
        var services = new ServiceCollection();
        services.AddScoped<Service>();
        var original = services.ToArray();
        Assert.ThrowsException<InvalidOperationException>(() => Scan(services).ProxySingleton(CreateProxy));
        CollectionAssert.AreEqual(original, services.ToArray());
    }

    [TestMethod]
    public void InvalidFactoryResultsAreRejectedAtResolution()
    {
        foreach (ServiceProxyFactory factory in new ServiceProxyFactory[]
            { (_, _, _) => null!, (_, _, _) => new object(), (_, target, _) => target })
        {
            var services = new ServiceCollection();
            services.AddSingleton(new State());
            Scan(services).ProxyScoped(factory);
            using var provider = services.BuildSimpleServiceProvider();
            using var scope = provider.CreateScope();
            Assert.ThrowsException<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IFirst>());
        }
    }

    [TestMethod]
    public void InterceptedScanExplainsMissingOptionalPackageWithoutPartialRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new State());
        var original = services.ToArray();
        var error = Assert.ThrowsException<InvalidOperationException>(() => Scan(services).InterceptedScoped());
        StringAssert.Contains(error.Message, "M.Simple.Services.Interception");
        CollectionAssert.AreEqual(original, services.ToArray());
    }

    [TestMethod]
    public void AssemblyEntryExplainsMissingRuntimeAndValidatesAssemblyArguments()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new State());
        var original = services.ToArray();
        var error = Assert.ThrowsException<InvalidOperationException>(() =>
            services.AddInterceptedAssemblyServices<IMarker>(typeof(Service).Assembly));
        StringAssert.Contains(error.Message, "M.Simple.Services.Interception");
        CollectionAssert.AreEqual(original, services.ToArray());
        Assert.ThrowsException<ArgumentException>(() => services.AddInterceptedAssemblyServices());
        Assert.ThrowsException<ArgumentNullException>(() => services.AddInterceptedAssemblyServices(null!));
        Assert.ThrowsException<ArgumentException>(() => services.AddInterceptedAssemblyServices(new Assembly[] { null! }));
    }
    private abstract class ServiceProviderBase : IDisposable
    {
        public abstract IServiceProvider Provider { get; }
        public abstract void Dispose();
    }
    private sealed class SimpleProvider(IServiceCollection services) : ServiceProviderBase
    {
        private readonly SimpleServiceProvider _provider = services.BuildSimpleServiceProvider();
        public override IServiceProvider Provider => _provider;
        public override void Dispose() => _provider.Dispose();
    }
    private sealed class StandardProvider(IServiceCollection services) : ServiceProviderBase
    {
        private readonly ServiceProvider _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        public override IServiceProvider Provider => _provider;
        public override void Dispose() => _provider.Dispose();
    }
}

