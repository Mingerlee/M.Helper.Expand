using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Services.Interception.Test;

[TestClass]
public class InterceptionTests
{
    public interface IBaseService { int Add(int value); }
    public interface IService : IBaseService
    {
        void Touch();
        Task CompleteAsync();
        Task<int> AddAsync(int value, CancellationToken cancellationToken = default);
        ValueTask CompleteValueAsync();
        ValueTask<int> AddValueAsync(int value);
        T Echo<T>(T value);
        Task<T> EchoAsync<T>(T value);
        void Fail();
        Task FailAsync();
        Task<int> FailResultAsync();
        ValueTask<int> FailValueAsync();
        Task<int> ThrowBeforeReturningTask();
    }

    public sealed class Service : IService, IDisposable
    {
        public int Calls;
        public int DisposeCalls;
        public Exception Error = new InvalidOperationException("business");
        public TaskCompletionSource Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Add(int value) { Calls++; return value + 1; }
        public void Touch() => Calls++;
        public async Task CompleteAsync() { Calls++; await Task.Yield(); }
        public async Task<int> AddAsync(int value, CancellationToken cancellationToken = default)
        {
            Calls++;
            await Gate.Task.WaitAsync(cancellationToken);
            return value + 1;
        }
        public async ValueTask CompleteValueAsync() { Calls++; await Task.Yield(); }
        public async ValueTask<int> AddValueAsync(int value) { Calls++; await Task.Yield(); return value + 1; }
        public T Echo<T>(T value) => value;
        public Task<T> EchoAsync<T>(T value) => Task.FromResult(value);
        public void Fail() => throw Error;
        public async Task FailAsync() { await Task.Yield(); throw Error; }
        public async Task<int> FailResultAsync() { await Task.Yield(); throw Error; }
        public async ValueTask<int> FailValueAsync() { await Task.Yield(); throw Error; }
        public Task<int> ThrowBeforeReturningTask() => throw Error;
        public void Dispose() => DisposeCalls++;
    }

    public sealed class Hook : MethodInterceptor, IDisposable
    {
        public List<string> Events { get; } = [];
        public List<MethodInvocationContext> Contexts { get; } = [];
        public bool ThrowBefore;
        public bool ThrowAfter;
        public int DisposeCalls;
        public override void Before(MethodInvocationContext context)
        {
            Events.Add("before:" + context.Method.Name);
            context.Items["started"] = true;
            if (ThrowBefore) throw new ApplicationException("before");
        }
        public override void After(MethodInvocationContext context)
        {
            Events.Add("after:" + context.Method.Name);
            Assert.AreEqual(true, context.Items["started"]);
            Contexts.Add(context);
            if (ThrowAfter) throw new ApplicationException("after");
        }
        public void Dispose() => DisposeCalls++;
    }

    [TestMethod]
    public void SyncVoidGenericAndInheritedMethods_PreserveResultsAndContext()
    {
        var target = new Service(); var hook = new Hook();
        var proxy = MethodHookProxy<IService>.Create(target, hook);
        Assert.AreEqual(5, proxy.Add(4));
        proxy.Touch();
        Assert.AreEqual("hello", proxy.Echo("hello"));
        var context = hook.Contexts[0];
        Assert.AreSame(target, context.Target);
        Assert.AreEqual(4, context.Arguments[0]);
        Assert.AreEqual(5, context.Result);
        Assert.IsNull(context.Exception);
        Assert.AreEqual(typeof(Service), context.ImplementationMethod.DeclaringType);
        CollectionAssert.AreEqual(new[] { "before:Add", "after:Add", "before:Touch", "after:Touch", "before:Echo", "after:Echo" }, hook.Events);
    }

    [TestMethod]
    public async Task AsyncMethods_WaitForCompletionBeforeAfterHook()
    {
        var target = new Service(); var hook = new Hook();
        var proxy = MethodHookProxy<IService>.Create(target, hook);
        var pending = proxy.AddAsync(4);
        Assert.IsFalse(pending.IsCompleted);
        CollectionAssert.AreEqual(new[] { "before:AddAsync" }, hook.Events);
        target.Gate.SetResult();
        Assert.AreEqual(5, await pending);
        Assert.AreEqual(5, hook.Contexts.Single().Result);
        await proxy.CompleteAsync();
        await proxy.CompleteValueAsync();
        Assert.AreEqual(9, await proxy.AddValueAsync(8));
        Assert.AreEqual("generic", await proxy.EchoAsync("generic"));
        Assert.AreEqual(5, hook.Contexts.Count);
    }

    [TestMethod]
    public async Task BusinessFailure_IsUnwrappedAndSkipsAfter()
    {
        var target = new Service(); var hook = new Hook { ThrowAfter = true };
        var proxy = MethodHookProxy<IService>.Create(target, hook);
        Assert.AreSame(target.Error, Assert.ThrowsException<InvalidOperationException>(() => proxy.Fail()));
        Assert.AreSame(target.Error, await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => proxy.FailAsync()));
        Assert.AreSame(target.Error, await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => proxy.FailResultAsync()));
        Assert.AreSame(target.Error, await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await proxy.FailValueAsync()));
        Assert.AreSame(target.Error, await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => proxy.ThrowBeforeReturningTask()));
        Assert.AreEqual(0, target.Error.Data.Count);
        Assert.AreEqual(0, hook.Contexts.Count);
        StringAssert.Contains(target.Error.StackTrace!, nameof(Service.ThrowBeforeReturningTask));
    }

    [TestMethod]
    public async Task BeforeFailure_ContinuesBusinessAndAfter()
    {
        var target = new Service(); var hook = new Hook { ThrowBefore = true };
        var proxy = MethodHookProxy<IService>.Create(target, hook);
        proxy.Touch();
        await proxy.CompleteAsync();
        Assert.AreEqual(2, await proxy.AddValueAsync(1));
        Assert.AreEqual(3, target.Calls);
        Assert.AreEqual(3, hook.Contexts.Count);
        Assert.IsTrue(hook.Contexts.All(c => c.BeforeException is ApplicationException));
    }

    [TestMethod]
    public async Task AfterFailure_PreservesSuccessfulBusinessResult()
    {
        var hook = new Hook { ThrowAfter = true };
        var proxy = MethodHookProxy<IService>.Create(new Service(), hook);
        Assert.AreEqual(2, proxy.Add(1));
        await proxy.CompleteAsync();
        Assert.IsTrue(hook.Contexts.All(c => c.Exception is null));
    }

    [TestMethod]
    public async Task Cancellation_RemainsCanceledAndSkipsAfter()
    {
        var hook = new Hook(); var proxy = MethodHookProxy<IService>.Create(new Service(), hook);
        using var source = new CancellationTokenSource();
        var pending = proxy.AddAsync(1, source.Token);
        source.Cancel();
        try { await pending; Assert.Fail("Expected cancellation"); }
        catch (OperationCanceledException error) { Assert.AreEqual(source.Token, error.CancellationToken); }
        Assert.IsTrue(pending.IsCanceled);
        Assert.AreEqual(0, hook.Contexts.Count);
    }

    public sealed class AsyncHook : MethodInterceptor
    {
        public List<string> Events { get; } = [];
        public override void Before(MethodInvocationContext context) => Events.Add("sync-before");
        public override void After(MethodInvocationContext context) => Events.Add("sync-after");
        public override async ValueTask BeforeAsync(MethodInvocationContext context)
        {
            await Task.Yield(); Events.Add("async-before");
        }
        public override async ValueTask AfterAsync(MethodInvocationContext context)
        {
            await Task.Yield(); Events.Add("async-after");
        }
    }

    [TestMethod]
    public async Task AsyncHooks_AreAwaitedWithoutBlockingSyncCalls()
    {
        var hook = new AsyncHook(); var proxy = MethodHookProxy<IService>.Create(new Service(), hook);
        await proxy.CompleteAsync(); proxy.Touch();
        CollectionAssert.AreEqual(new[] { "async-before", "async-after", "sync-before", "sync-after" }, hook.Events);
    }

    [MethodHook]
    public interface IMarked
    {
        int Normal();
        [SkipMethodHook] int Skip();
        int ImplementationSkip();
    }
    public sealed class Marked : IMarked
    {
        public int Normal() => 1;
        public int Skip() => 2;
        [SkipMethodHook] public int ImplementationSkip() => 3;
    }
    [SkipMethodHook]
    public interface ISelective
    {
        [MethodHook] int Included();
        int Excluded();
        [MethodHook] int Both();
    }
    public sealed class Selective : ISelective
    {
        int ISelective.Included() => 1;
        public int Excluded() => 2;
        [SkipMethodHook] public int Both() => 3;
    }

    [TestMethod]
    public void Attributes_SupportInterfaceImplementationAndMethodOverrides()
    {
        var hook = new Hook();
        var marked = MethodHookProxy<IMarked>.Create(new Marked(), hook, attributedOnly: true);
        Assert.AreEqual(1, marked.Normal()); Assert.AreEqual(2, marked.Skip()); Assert.AreEqual(3, marked.ImplementationSkip());
        Assert.AreEqual(1, hook.Contexts.Count);
        var selective = MethodHookProxy<ISelective>.Create(new Selective(), hook, attributedOnly: true);
        Assert.AreEqual(1, selective.Included()); Assert.AreEqual(2, selective.Excluded()); Assert.AreEqual(3, selective.Both());
        Assert.AreEqual(2, hook.Contexts.Count);
        var noAttributes = MethodHookProxy<IService>.Create(new Service(), hook, attributedOnly: true);
        noAttributes.Add(1);
        Assert.AreEqual(2, hook.Contexts.Count);
    }

    [TestMethod]
    public void ScopedRegistration_SharesWithinScopeAndDisposesTargetExactlyOnce()
    {
        var services = new ServiceCollection();
        services.AddInterceptedScoped<IService, Service, Hook>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        Service target; Hook hook;
        using (var scope = provider.CreateScope())
        {
            var first = scope.ServiceProvider.GetRequiredService<IService>();
            Assert.AreSame(first, scope.ServiceProvider.GetRequiredService<IService>());
            target = scope.ServiceProvider.GetRequiredService<Service>();
            hook = scope.ServiceProvider.GetRequiredService<Hook>();
            first.Touch(); Assert.AreEqual(1, target.Calls);
            using var other = provider.CreateScope();
            Assert.AreNotSame(first, other.ServiceProvider.GetRequiredService<IService>());
        }
        Assert.AreEqual(1, target.DisposeCalls); Assert.AreEqual(1, hook.DisposeCalls);
    }

    [TestMethod]
    public void SingletonAndTransientRegistration_RespectLifetimes()
    {
        var singletons = new ServiceCollection().AddInterceptedSingleton<IService, Service, Hook>();
        using var singletonProvider = singletons.BuildServiceProvider();
        using var scope = singletonProvider.CreateScope();
        Assert.AreSame(singletonProvider.GetRequiredService<IService>(), scope.ServiceProvider.GetRequiredService<IService>());
        using var transientProvider = new ServiceCollection().AddInterceptedTransient<IService, Service, Hook>().BuildServiceProvider();
        Assert.AreNotSame(transientProvider.GetRequiredService<IService>(), transientProvider.GetRequiredService<IService>());
    }

    public interface IByRef { void Modify(ref int number); }
    public interface IStream { IAsyncEnumerable<int> Stream(); }
    public interface IAsyncVoid { void Run(); }
    public sealed class AsyncVoid : IAsyncVoid { public async void Run() => await Task.Yield(); }

    [TestMethod]
    public void UnsupportedSignatures_AreRejectedBeforeInvocation()
    {
        Assert.ThrowsException<NotSupportedException>(() => MethodHookProxy<IDisposable>.Create(new Service(), new Hook()));
        Assert.ThrowsException<NotSupportedException>(() => MethodHookProxy<IAsyncVoid>.Create(new AsyncVoid(), new Hook()));
        // Contract validation can run before a concrete target is even resolved.
        Assert.ThrowsException<NotSupportedException>(() => new ServiceCollection().AddInterceptedScoped<IByRef, ByRef, Hook>());
        Assert.ThrowsException<NotSupportedException>(() => new ServiceCollection().AddInterceptedScoped<IStream, StreamService, Hook>());
    }
    public sealed class ByRef : IByRef { public void Modify(ref int number) => number++; }
    public sealed class StreamService : IStream
    {
        public async IAsyncEnumerable<int> Stream() { await Task.Yield(); yield return 1; }
    }

    public interface IImplementationRules
    {
        void First();
        void Second();
    }
    [MethodHook]
    public sealed class TypeMarked : IImplementationRules
    {
        public void First() { }
        [SkipMethodHook] public void Second() { }
    }
    public sealed class MethodMarked : IImplementationRules
    {
        [MethodHook] public void First() { }
        public void Second() { }
    }

    [TestMethod]
    public void ImplementationAttributes_SupportClassAndMethodMarking()
    {
        var hook = new Hook();
        var typeMarked = MethodHookProxy<IImplementationRules>.Create(new TypeMarked(), hook, attributedOnly: true);
        typeMarked.First(); typeMarked.Second();
        var methodMarked = MethodHookProxy<IImplementationRules>.Create(new MethodMarked(), hook, attributedOnly: true);
        methodMarked.First(); methodMarked.Second();
        Assert.AreEqual(2, hook.Contexts.Count);
        Assert.IsTrue(hook.Contexts.All(c => c.Method.Name == nameof(IImplementationRules.First)));
    }

    public interface IInternalCalls { void Outer(); void Inner(); }
    public sealed class InternalCalls : IInternalCalls
    {
        public int InnerCalls;
        public void Outer() => Inner();
        public void Inner() => InnerCalls++;
    }

    [TestMethod]
    public void SelfInvocationAndDirectCalls_DoNotEnterProxy()
    {
        var hook = new Hook(); var target = new InternalCalls();
        var proxy = MethodHookProxy<IInternalCalls>.Create(target, hook);
        proxy.Outer(); target.Outer();
        Assert.AreEqual(2, target.InnerCalls);
        Assert.AreEqual(1, hook.Contexts.Count);
        Assert.AreEqual(nameof(IInternalCalls.Outer), hook.Contexts.Single().Method.Name);
    }

    public sealed class ConcurrentHook : MethodInterceptor
    {
        public System.Collections.Concurrent.ConcurrentQueue<MethodInvocationContext> Completed { get; } = new();
        public override async ValueTask BeforeAsync(MethodInvocationContext context)
        {
            context.Items["value"] = context.Arguments[0];
            await Task.Yield();
        }
        public override async ValueTask AfterAsync(MethodInvocationContext context)
        {
            await Task.Yield();
            Completed.Enqueue(context);
        }
    }

    [TestMethod]
    public async Task ConcurrentInvocations_HaveIndependentContexts()
    {
        var target = new Service(); target.Gate.SetResult();
        var hook = new ConcurrentHook(); var proxy = MethodHookProxy<IService>.Create(target, hook);
        var results = await Task.WhenAll(Enumerable.Range(1, 50).Select(n => proxy.AddAsync(n)));
        CollectionAssert.AreEqual(Enumerable.Range(2, 50).ToArray(), results);
        Assert.AreEqual(50, hook.Completed.Distinct().Count());
        Assert.IsTrue(hook.Completed.All(c => (int)c.Result! == (int)c.Items["value"]! + 1));
    }

    public interface IValueFailure { ValueTask RunAsync(); }
    public sealed class ValueFailure : IValueFailure
    {
        public async ValueTask RunAsync() { await Task.Yield(); throw new InvalidOperationException("value failure"); }
    }

    [TestMethod]
    public async Task NonGenericValueTaskFailure_SkipsAfter()
    {
        var hook = new Hook(); var proxy = MethodHookProxy<IValueFailure>.Create(new ValueFailure(), hook);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await proxy.RunAsync());
        Assert.AreEqual(0, hook.Contexts.Count);
    }

    public interface ICustomAwaitable { System.Runtime.CompilerServices.TaskAwaiter Run(); }
    public interface ICustomAwaitableResult { CustomAwaitable Run(); }
    public sealed class CustomAwaitable
    {
        public System.Runtime.CompilerServices.TaskAwaiter GetAwaiter() => Task.CompletedTask.GetAwaiter();
    }
    public sealed class CustomAwaitableService : ICustomAwaitableResult
    {
        public CustomAwaitable Run() => new();
    }

    [TestMethod]
    public void CustomAwaitable_IsRejectedRatherThanRunningAfterPrematurely()
    {
        Assert.ThrowsException<NotSupportedException>(() => MethodHookProxy<ICustomAwaitableResult>.Create(new CustomAwaitableService(), new Hook()));
    }
}
