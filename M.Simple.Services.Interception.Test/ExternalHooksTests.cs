using M.Simple.Container;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Services.Interception.Test;

[TestClass]
public sealed class ExternalHooksTests
{
    public sealed class State { public int Before; public int After; public int Disposed; }
    public sealed class Hooks(State state) : IDisposable
    {
        public bool FailBefore;
        public bool FailAfter;
        public void Before(MethodInvocationContext context)
        {
            state.Before++;
            if (FailBefore) throw new ApplicationException("before");
            context.SetArgument("value", context.GetArgument<int>("value") + 1);
        }
        public async ValueTask BeforeAsync(MethodInvocationContext context)
        {
            await Task.Yield();
            Before(context);
        }
        public void After(MethodInvocationContext context)
        {
            state.After++;
            context.SetResult((int)context.Result! + 10);
            if (FailAfter) throw new ApplicationException("after");
        }
        public Task AfterAsync(MethodInvocationContext context) { After(context); return Task.CompletedTask; }
        public void Dispose() => state.Disposed++;
    }
    public interface IService
    {
        int Run(int value);
        Task<int> RunAsync(int value);
        ValueTask<int> RunValueAsync(int value);
    }
    public sealed class Service : IService
    {
        public bool Fail;
        public readonly Exception Error = new InvalidOperationException("business");
        [BeforeMethod(typeof(Hooks), nameof(Hooks.Before)), AfterMethod(typeof(Hooks), nameof(Hooks.After))]
        public int Run(int value) => Fail ? throw Error : value * 2;
        [BeforeMethod(typeof(Hooks), nameof(Hooks.BeforeAsync)), AfterMethod(typeof(Hooks), nameof(Hooks.AfterAsync))]
        public async Task<int> RunAsync(int value) { await Task.Yield(); return Run(value); }
        [BeforeMethod(typeof(Hooks), nameof(Hooks.BeforeAsync)), AfterMethod(typeof(Hooks), nameof(Hooks.AfterAsync))]
        public async ValueTask<int> RunValueAsync(int value) { await Task.Yield(); return Run(value); }
    }
    private sealed class LogSink : ILogger, ILoggerFactory
    {
        public readonly List<Exception> Errors = [];
        public ILogger CreateLogger(string categoryName) => this;
        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        { if (exception is not null) Errors.Add(exception); }
    }

    [TestMethod]
    public async Task ExternalHooksUseScopeDependenciesAndLogFailuresWithoutChangingBusinessOutcome()
    {
        var services = new ServiceCollection();
        services.AddScoped<State>();
        services.AddScoped<Hooks>();
        var logs = new LogSink();
        services.AddSingleton<ILoggerFactory>(logs);
        services.AddInterceptedScoped<IService, Service>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        State state;
        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var proxy = sp.GetRequiredService<IService>();
            state = sp.GetRequiredService<State>();
            var hooks = sp.GetRequiredService<Hooks>();
            Assert.AreEqual(16, proxy.Run(2));
            Assert.AreEqual(16, await proxy.RunAsync(2));
            Assert.AreEqual(16, await proxy.RunValueAsync(2));
            Assert.AreEqual(3, state.Before); Assert.AreEqual(3, state.After);
            hooks.FailBefore = true; hooks.FailAfter = true;
            Assert.AreEqual(4, proxy.Run(2));
            Assert.AreEqual(4, await proxy.RunAsync(2));
            Assert.AreEqual(4, await proxy.RunValueAsync(2));
            Assert.AreEqual(6, logs.Errors.Count);
            hooks.FailBefore = false;
            var target = sp.GetRequiredService<Service>(); target.Fail = true;
            Assert.AreSame(target.Error, Assert.ThrowsException<InvalidOperationException>(() => proxy.Run(2)));
            Assert.AreSame(target.Error, await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => proxy.RunAsync(2)));
            Assert.AreEqual(6, state.After);
            using var other = provider.CreateScope();
            Assert.AreNotSame(hooks, other.ServiceProvider.GetRequiredService<Hooks>());
        }
        Assert.AreEqual(1, state.Disposed);
    }

    [TestMethod]
    public void MissingHookAndDirectOrIndirectScopedCaptureAreRejected()
    {
        var missing = new ServiceCollection().AddInterceptedScoped<IService, Service>();
        using (var provider = missing.BuildServiceProvider())
        using (var scope = provider.CreateScope())
            StringAssert.Contains(Assert.ThrowsException<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IService>()).Message, nameof(Hooks));
        var direct = new ServiceCollection();
        direct.AddScoped<State>(); direct.AddScoped<Hooks>();
        StringAssert.Contains(Assert.ThrowsException<InvalidOperationException>(() => direct.AddInterceptedSingleton<IService, Service>()).Message, "Scoped");
        var indirect = new ServiceCollection();
        indirect.AddScoped<State>(); indirect.AddTransient<Hooks>();
        StringAssert.Contains(Assert.ThrowsException<InvalidOperationException>(() => indirect.AddInterceptedSingleton<IService, Service>()).Message, nameof(State));
        var late = new ServiceCollection().AddInterceptedSingleton<IService, Service>();
        late.AddScoped<State>(); late.AddTransient<Hooks>();
        using var lateProvider = late.BuildServiceProvider(); // Independent of provider ValidateScopes settings.
        StringAssert.Contains(Assert.ThrowsException<InvalidOperationException>(() => lateProvider.GetRequiredService<IService>()).Message, "Scoped");
    }

    [TestMethod]
    public async Task AssemblyScanIsIdempotentAndCompatibleWithSimpleContainer()
    {
        var services = new ServiceCollection();
        var assembly = typeof(global::MessageService).Assembly;
        services.AddInterceptedAssemblyServices(assembly);
        var count = services.Count;
        services.AddInterceptedAssemblyServices(assembly);
        Assert.AreEqual(count, services.Count);
        services.AddScoped<global::MessageHooks>(); // Hook registrations can follow the scan.
        using var provider = services.BuildSimpleServiceProvider();
        using var scope = provider.CreateScope();
        var message = scope.ServiceProvider.GetRequiredService<global::IMessageService>();
        var plain = scope.ServiceProvider.GetRequiredService<global::IValueService<int>>();
        Assert.AreEqual(42, plain.Echo(42));
        Assert.IsInstanceOfType(plain, typeof(global::ValueService<int>));
        Assert.AreSame(message, scope.ServiceProvider.GetRequiredService<global::IMessageService>());
        Assert.AreEqual("hello【前置追加】【后置追加】", await message.SayHolle("hello"));
        Assert.AreEqual(1, scope.ServiceProvider.GetServices<global::IMessageService>().Count());
        using var other = provider.CreateScope();
        Assert.AreNotSame(message, other.ServiceProvider.GetRequiredService<global::IMessageService>());
    }

    public interface IAction { void Run(); }
    public sealed class InvalidExternal : IAction
    {
        [BeforeMethod(typeof(Hooks), "Missing")]
        public void Run() { }
    }
    public sealed class CancelBefore : MethodInterceptor
    {
        public int AfterCalls;
        public override void Before(MethodInvocationContext context)
            => throw new OperationCanceledException(context.CancellationToken);
        public override void After(MethodInvocationContext context) => AfterCalls++;
    }

    [TestMethod]
    public void ConfigurationErrorsAreNotSwallowedAndCanceledRequestsDoNotRunAfter()
    {
        var services = new ServiceCollection();
        services.AddScoped<State>(); services.AddScoped<Hooks>();
        services.AddInterceptedScoped<IAction, InvalidExternal>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.ThrowsException<MissingMethodException>(() => scope.ServiceProvider.GetRequiredService<IAction>());
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        var hook = new CancelBefore();
        var proxy = MethodHookProxy<InterceptionTests.IService>.Create(new InterceptionTests.Service(), hook);
        Assert.ThrowsException<OperationCanceledException>(() => proxy.AddAsync(1, canceled.Token).GetAwaiter().GetResult());
        Assert.AreEqual(0, hook.AfterCalls);
    }
}
