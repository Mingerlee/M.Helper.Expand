using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Services.Interception.Test;

[TestClass]
public class NamedHooksTests
{
    public interface IService
    {
        int Run(int value);
        Task<int> RunAsync(int value, CancellationToken cancellationToken = default);
        ValueTask<int> RunValueAsync(int value);
        void BeforeOnly();
        void AfterOnly();
        [BeforeMethod("DoesNotExist"), SkipMethodHook] void Skipped();
        T Echo<T>(T value);
    }

    public sealed class Service : IService
    {
        public List<string> Events { get; } = [];
        public List<MethodInvocationContext> Completed { get; } = [];
        public bool FailBefore;
        public bool FailBusiness;
        public bool FailAfter;
        public TaskCompletionSource BeforeGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Exception BusinessError = new InvalidOperationException("business");
        [BeforeMethod(nameof(Before)), AfterMethod(nameof(After))]
        public int Run(int value)
        {
            Events.Add("business");
            if (FailBusiness) throw BusinessError;
            return value * 2;
        }
        [BeforeMethod(nameof(BeforeAsync)), AfterMethod(nameof(AfterAsync))]
        public async Task<int> RunAsync(int value, CancellationToken cancellationToken = default)
        {
            Events.Add("business");
            await Task.Delay(1, cancellationToken);
            if (FailBusiness) throw BusinessError;
            return value * 2;
        }
        [BeforeMethod(nameof(Before)), AfterMethod(nameof(AfterAsync))]
        public async ValueTask<int> RunValueAsync(int value)
        {
            Events.Add("business"); await Task.Yield(); return value * 2;
        }
        [BeforeMethod(nameof(Before))] public void BeforeOnly() => Events.Add("business");
        [AfterMethod(nameof(After))] public void AfterOnly() => Events.Add("business");
        public void Skipped() => Events.Add("business");
        [BeforeMethod(nameof(Before)), AfterMethod(nameof(After))]
        public T Echo<T>(T value) { Events.Add("business"); return value; }
        private void Before(MethodInvocationContext context)
        {
            Events.Add("before");
            context.Items["named"] = context.Method.Name;
            if (FailBefore) throw new ApplicationException("before");
        }
        private void After(MethodInvocationContext context)
        {
            Events.Add("after"); Completed.Add(context);
            if (FailAfter) throw new ApplicationException("after");
        }
        private async Task BeforeAsync(MethodInvocationContext context)
        {
            Events.Add("before-wait"); await BeforeGate.Task; Before(context);
        }
        private async ValueTask AfterAsync(MethodInvocationContext context)
        {
            await Task.Yield(); After(context);
        }
    }

    [TestMethod]
    public void NamedHooks_EnableInterceptionAndReadArgumentsResults()
    {
        var target = new Service(); var proxy = MethodHookProxy<IService>.Create(target);
        Assert.AreEqual(8, proxy.Run(4));
        CollectionAssert.AreEqual(new[] { "before", "business", "after" }, target.Events);
        var context = target.Completed.Single();
        Assert.AreEqual(4, context.Arguments[0]); Assert.AreEqual(8, context.Result);
        Assert.AreEqual(nameof(IService.Run), context.Items["named"]);
        Assert.AreEqual("generic", proxy.Echo("generic"));
        Assert.AreEqual(2, target.Completed.Count);
    }

    [TestMethod]
    public async Task AsyncNamedHooks_AreAwaitedAndSupportValueTask()
    {
        var target = new Service(); var proxy = MethodHookProxy<IService>.Create(target);
        var pending = proxy.RunAsync(3);
        CollectionAssert.AreEqual(new[] { "before-wait" }, target.Events);
        Assert.IsFalse(pending.IsCompleted);
        target.BeforeGate.SetResult(); Assert.AreEqual(6, await pending);
        CollectionAssert.AreEqual(new[] { "before-wait", "before", "business", "after" }, target.Events);
        Assert.AreEqual(10, await proxy.RunValueAsync(5));
        Assert.AreEqual(10, target.Completed.Last().Result);
    }

    [TestMethod]
    public void BeforeFailure_StopsBusinessAndAllAfterHooks()
    {
        var target = new Service { FailBefore = true };
        var proxy = MethodHookProxy<IService>.Create(target);
        Assert.AreEqual("before", Assert.ThrowsException<ApplicationException>(() => proxy.Run(1)).Message);
        CollectionAssert.AreEqual(new[] { "before" }, target.Events);
    }

    [TestMethod]
    public async Task BusinessFailureAndCancellation_ExecuteNamedAfterAndPreserveOriginalError()
    {
        var target = new Service { FailBusiness = true, FailAfter = true }; target.BeforeGate.SetResult();
        var proxy = MethodHookProxy<IService>.Create(target);
        Assert.AreSame(target.BusinessError, Assert.ThrowsException<InvalidOperationException>(() => proxy.Run(1)));
        Assert.AreSame(target.BusinessError, await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => proxy.RunAsync(1)));
        Assert.AreSame(target.BusinessError, target.Completed.Last().Exception);
        Assert.IsInstanceOfType(target.BusinessError.Data[MethodHookProxy<IService>.AfterExceptionDataKey], typeof(ApplicationException));
        target.FailAfter = false;
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var task = proxy.RunAsync(2, cancellation.Token);
        try { await task; Assert.Fail("Expected cancellation"); } catch (OperationCanceledException) { }
        Assert.IsTrue(task.IsCanceled); Assert.IsTrue(target.Completed.Last().IsCanceled);
    }

    [TestMethod]
    public void BeforeOnlyAfterOnlyAndSkip_AreIndependent()
    {
        var target = new Service(); var proxy = MethodHookProxy<IService>.Create(target);
        proxy.BeforeOnly(); proxy.AfterOnly(); proxy.Skipped();
        CollectionAssert.AreEqual(new[] { "before", "business", "business", "after", "business" }, target.Events);
    }

    public sealed class GeneralHook(Service target) : MethodInterceptor
    {
        public bool FailAfter;
        public override void Before(MethodInvocationContext context) => target.Events.Add("general-before");
        public override void After(MethodInvocationContext context)
        {
            target.Events.Add("general-after");
            if (FailAfter) throw new NotSupportedException("general-after");
        }
    }

    [TestMethod]
    public void GeneralAndNamedHooks_ComposeInOrderAndBothAfterHooksRun()
    {
        var target = new Service(); var hook = new GeneralHook(target);
        var proxy = MethodHookProxy<IService>.Create(target, hook, attributedOnly: true);
        proxy.Run(1);
        CollectionAssert.AreEqual(new[] { "general-before", "before", "business", "after", "general-after" }, target.Events);
        target.FailAfter = true; hook.FailAfter = true;
        var aggregate = Assert.ThrowsException<AggregateException>(() => proxy.Run(2));
        Assert.AreEqual(2, aggregate.InnerExceptions.Count);
        Assert.AreEqual("general-after", target.Events.Last());
        target.FailBusiness = true;
        Assert.AreSame(target.BusinessError, Assert.ThrowsException<InvalidOperationException>(() => proxy.Run(2)));
        Assert.IsInstanceOfType(target.BusinessError.Data[MethodHookProxy<IService>.AfterExceptionDataKey], typeof(AggregateException));
    }

    public interface IOverridden { [BeforeMethod("MissingInterfaceHook")] int Run(); }
    public class Parent
    {
        public const string HookName = nameof(PrivateBefore);
        public int HookCalls;
        private void PrivateBefore(MethodInvocationContext context) => HookCalls++;
    }
    public sealed class Child : Parent, IOverridden
    {
        [BeforeMethod(HookName)] int IOverridden.Run() => 10;
    }

    [TestMethod]
    public void ImplementationAttributeOverridesInterfaceAndCanFindPrivateBaseHook()
    {
        var target = new Child(); var proxy = MethodHookProxy<IOverridden>.Create(target);
        Assert.AreEqual(10, proxy.Run()); Assert.AreEqual(1, target.HookCalls);
    }

    public interface IAction { void Run(); }
    public sealed class Missing : IAction { [BeforeMethod("Missing")] public void Run() { } }
    public sealed class Blank : IAction { [BeforeMethod(" ")] public void Run() { } }
    public sealed class WrongParameters : IAction
    {
        [BeforeMethod(nameof(Before))] public void Run() { }
        private void Before() { }
    }
    public sealed class StaticHook : IAction
    {
        [BeforeMethod(nameof(Before))] public void Run() { }
        private static void Before(MethodInvocationContext context) { }
    }
    public sealed class AsyncOnSync : IAction
    {
        [BeforeMethod(nameof(Before))] public void Run() { }
        private Task Before(MethodInvocationContext context) => Task.CompletedTask;
    }
    public sealed class AsyncVoidHook : IAction
    {
        [AfterMethod(nameof(After))] public void Run() { }
        private async void After(MethodInvocationContext context) => await Task.Yield();
    }
    public sealed class WrongReturn : IAction
    {
        [AfterMethod(nameof(After))] public void Run() { }
        private int After(MethodInvocationContext context) => 1;
    }

    [TestMethod]
    public void InvalidConfiguration_IsRejectedAtProxyCreation()
    {
        Assert.ThrowsException<MissingMethodException>(() => MethodHookProxy<IAction>.Create(new Missing()));
        foreach (var target in new IAction[] { new Blank(), new WrongParameters(), new StaticHook(), new AsyncOnSync(), new AsyncVoidHook(), new WrongReturn() })
            Assert.ThrowsException<ArgumentException>(() => MethodHookProxy<IAction>.Create(target), target.GetType().Name);
    }

    [TestMethod]
    public void NewDiOverloads_DoNotRequireAnInterceptorClass()
    {
        using var scoped = new ServiceCollection().AddInterceptedScoped<IService, Service>().BuildServiceProvider();
        using var scope = scoped.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IService>();
        Assert.AreSame(service, scope.ServiceProvider.GetRequiredService<IService>()); Assert.AreEqual(6, service.Run(3));
        using var single = new ServiceCollection().AddInterceptedSingleton<IService, Service>().BuildServiceProvider();
        Assert.AreSame(single.GetRequiredService<IService>(), single.GetRequiredService<IService>());
        using var transient = new ServiceCollection().AddInterceptedTransient<IService, Service>().BuildServiceProvider();
        Assert.AreNotSame(transient.GetRequiredService<IService>(), transient.GetRequiredService<IService>());
    }

    public interface IInterfaceNamed { [BeforeMethod("Before")] void Run(); }
    public sealed class InterfaceNamed : IInterfaceNamed
    {
        public int BeforeCalls;
        private void Before(MethodInvocationContext context) => BeforeCalls++;
        public void Run() { }
    }
    public interface ISelfHook { void Run(MethodInvocationContext context); }
    public sealed class SelfHook : ISelfHook
    {
        [BeforeMethod(nameof(Run))] public void Run(MethodInvocationContext context) { }
    }

    [TestMethod]
    public void InterfaceNamedHooks_WorkAndSelfHookIsRejected()
    {
        var target = new InterfaceNamed(); MethodHookProxy<IInterfaceNamed>.Create(target).Run();
        Assert.AreEqual(1, target.BeforeCalls);
        Assert.ThrowsException<ArgumentException>(() => MethodHookProxy<ISelfHook>.Create(new SelfHook()));
    }
}
