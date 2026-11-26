using M.Simple.Container;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Services.Interception.Test;

[TestClass]
public sealed class RuntimeScanningTests
{
    public interface IInterception { }
    public interface IMessage
    {
        string Say(string message);
        Task<string> SayAsync(string message);
        ValueTask<string> SayValueAsync(string message);
        void Fail();
    }
    public interface IAudit { Guid Id { get; } }
    public sealed class State { public int Disposed; }
    public sealed class Message(State state) : IMessage, IAudit, IInterception, IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();
        [BeforeMethod(nameof(Before)), AfterMethod(nameof(After))]
        public string Say(string message) => message;
        [BeforeMethod(nameof(Before)), AfterMethod(nameof(After))]
        public async Task<string> SayAsync(string message) { await Task.Yield(); return message; }
        [BeforeMethod(nameof(Before)), AfterMethod(nameof(After))]
        public async ValueTask<string> SayValueAsync(string message) { await Task.Yield(); return message; }
        public void Fail() => throw new ApplicationException("business");
        private void Before(MethodInvocationContext context) => context.SetArgument("message", context.GetArgument<string>("message") + ":before");
        private void After(MethodInvocationContext context) => context.SetResult(context.Result + ":after");
        public void Dispose() => state.Disposed++;
    }

    private static AssemblyRegistrationBuilder Scan(IServiceCollection services)
        => services.Scan(typeof(Message).Assembly).AssignableTo<IInterception>()
            .ExcludeInterface(typeof(IInterception)).AsImplementedInterfaces();

    [DataTestMethod]
    [DataRow(false, ServiceLifetime.Scoped)]
    [DataRow(true, ServiceLifetime.Scoped)]
    [DataRow(false, ServiceLifetime.Transient)]
    [DataRow(true, ServiceLifetime.Transient)]
    [DataRow(false, ServiceLifetime.Singleton)]
    [DataRow(true, ServiceLifetime.Singleton)]
    public async Task ExactFluentApiRunsHooksAndRespectsLifetime(bool simple, ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        var state = new State();
        services.AddSingleton(state);
        void Register()
        {
            var scan = Scan(services).AsSelf();
            switch (lifetime)
            {
                case ServiceLifetime.Singleton: scan.InterceptedSingleton(); break;
                case ServiceLifetime.Transient: scan.InterceptedTransient(); break;
                default: scan.InterceptedScoped(); break;
            }
        }
        Register();
        var count = services.Count;
        Register();
        Assert.AreEqual(count, services.Count);
        IServiceProvider root = simple ? services.BuildSimpleServiceProvider()
            : services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using ((IDisposable)root)
        using (var scope = root.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var message = sp.GetRequiredService<IMessage>();
            Assert.IsFalse(message is Message);
            Assert.AreEqual("hello:before:after", message.Say("hello"));
            Assert.AreEqual("hello:before:after", await message.SayAsync("hello"));
            Assert.AreEqual("hello:before:after", await message.SayValueAsync("hello"));
            Assert.AreEqual("business", Assert.ThrowsException<ApplicationException>(() => message.Fail()).Message);
            Assert.IsFalse(sp.GetServices<IInterception>().Any());
            Assert.AreEqual(1, sp.GetServices<IMessage>().Count());
            var repeated = sp.GetRequiredService<IMessage>();
            if (lifetime == ServiceLifetime.Transient) Assert.AreNotSame(message, repeated);
            else
            {
                Assert.AreSame(message, repeated);
                Assert.AreEqual(sp.GetRequiredService<Message>().Id, sp.GetRequiredService<IAudit>().Id);
            }
            using var other = root.CreateScope();
            if (lifetime == ServiceLifetime.Singleton) Assert.AreSame(message, other.ServiceProvider.GetRequiredService<IMessage>());
            else Assert.AreNotSame(message, other.ServiceProvider.GetRequiredService<IMessage>());
        }
        Assert.AreEqual(lifetime == ServiceLifetime.Singleton ? 1 : lifetime == ServiceLifetime.Scoped ? 2 : 4, state.Disposed);
    }

    public sealed class Hook(State state)
    {
        public void Before(MethodInvocationContext context) { _ = state; context.SetArgument("message", "external"); }
    }
    public interface IExternal { string Say(string message); }
    public sealed class External : IExternal
    {
        [BeforeMethod(typeof(Hook), nameof(Hook.Before))]
        public string Say(string message) => message;
    }

    [TestMethod]
    public void ExternalHooksAddedAfterScanAreResolvedAndSingletonCaptureIsRejected()
    {
        var scoped = new ServiceCollection();
        scoped.Scan(typeof(External).Assembly).Where(t => t == typeof(External)).InterceptedScoped();
        scoped.AddScoped<State>(); scoped.AddScoped<Hook>();
        using (var root = scoped.BuildSimpleServiceProvider())
        using (var scope = root.CreateScope())
            Assert.AreEqual("external", scope.ServiceProvider.GetRequiredService<IExternal>().Say("hello"));

        var singleton = new ServiceCollection();
        singleton.Scan(typeof(External).Assembly).Where(t => t == typeof(External)).InterceptedSingleton();
        singleton.AddScoped<State>(); singleton.AddTransient<Hook>();
        using var provider = singleton.BuildServiceProvider(); // Verify Interception's own validation, even without ValidateScopes.
        StringAssert.Contains(Assert.ThrowsException<InvalidOperationException>(() => provider.GetRequiredService<IExternal>()).Message, "Scoped");
    }

    public interface IInvalid { void Run(ref int value); }
    public sealed class ZInvalid : IInvalid { public void Run(ref int value) { } }

    [TestMethod]
    public void InvalidContractsPreserveTheOriginalServiceCollectionAndExceptionType()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new State());
        var original = services.ToArray();
        Assert.ThrowsException<NotSupportedException>(() => services.Scan(typeof(Message).Assembly)
            .Where(t => t == typeof(Message) || t == typeof(ZInvalid)).InterceptedScoped());
        CollectionAssert.AreEqual(original, services.ToArray());
    }
}
