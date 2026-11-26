using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Services.Interception.Test;

[TestClass]
public class MutationTests
{
    public interface IService
    {
        string Run(string msg);
        Task<string> RunAsync(string msg);
        ValueTask<string> RunValueAsync(string msg);
        T Echo<T>(T value);
        int? Nullable(int? value);
        void Nothing();
        Task NothingAsync();
        Task FailAsync();
        Task CancelAsync(CancellationToken cancellationToken);
    }
    public sealed class Service : IService
    {
        public string Run(string msg) => msg;
        public async Task<string> RunAsync(string msg) { await Task.Yield(); return msg; }
        public async ValueTask<string> RunValueAsync(string msg) { await Task.Yield(); return msg; }
        public T Echo<T>(T value) => value;
        public int? Nullable(int? value) => value;
        public void Nothing() { }
        public Task NothingAsync() => Task.CompletedTask;
        public Task FailAsync() => Task.FromException(new ApplicationException("business"));
        public Task CancelAsync(CancellationToken cancellationToken) => Task.Delay(1, cancellationToken);
    }
    public sealed class Hook : MethodInterceptor
    {
        public Action<MethodInvocationContext>? OnBefore;
        public Action<MethodInvocationContext>? OnAfter;
        public MethodInvocationContext? Last;
        public override void Before(MethodInvocationContext context) { Last = context; OnBefore?.Invoke(context); }
        public override void After(MethodInvocationContext context) => OnAfter?.Invoke(context);
    }

    [TestMethod]
    public async Task BeforeArgumentsAndAfterResults_AffectSyncTaskAndValueTask()
    {
        var hook = new Hook
        {
            OnBefore = c => c.SetArgument("msg", c.GetArgument<string>("msg") + "【前置追加】"),
            OnAfter = c => c.SetResult((string)c.Result! + "【后置追加】")
        };
        var proxy = MethodHookProxy<IService>.Create(new Service(), hook);
        const string expected = "你好【前置追加】【后置追加】";
        Assert.AreEqual(expected, proxy.Run("你好"));
        Assert.AreEqual(expected, await proxy.RunAsync("你好"));
        Assert.AreEqual(expected, await proxy.RunValueAsync("你好"));
        Assert.ThrowsException<InvalidOperationException>(() => hook.Last!.SetResult("late"));
        Assert.ThrowsException<InvalidOperationException>(() => hook.Last!.SetArgument("msg", "late"));
    }

    [TestMethod]
    public void TypesNullGenericAndUnknownNames_AreValidated()
    {
        var hook = new Hook(); var proxy = MethodHookProxy<IService>.Create(new Service(), hook);
        hook.OnBefore = c =>
        {
            Assert.ThrowsException<ArgumentException>(() => c.SetArgument("missing", 1));
            Assert.ThrowsException<ArgumentException>(() => c.GetArgument<int>("missing"));
            Assert.ThrowsException<ArgumentException>(() => c.SetArgument("value", "wrong"));
            Assert.ThrowsException<ArgumentException>(() => c.SetArgument("value", null));
            Assert.ThrowsException<InvalidOperationException>(() => c.SetResult(1));
            c.SetArgument("value", 7);
        };
        hook.OnAfter = c =>
        {
            Assert.ThrowsException<ArgumentException>(() => c.SetResult("wrong"));
            Assert.ThrowsException<ArgumentException>(() => c.SetResult(null));
            Assert.ThrowsException<InvalidOperationException>(() => c.SetArgument("value", 1));
            c.SetResult(9);
        };
        Assert.AreEqual(9, proxy.Echo(1));
        hook.OnBefore = c => c.SetArgument("value", null);
        hook.OnAfter = c => { Assert.IsNull(c.Result); c.SetResult(3); };
        Assert.AreEqual(3, proxy.Nullable(1));
        hook.OnBefore = c => c.SetArgument("msg", null);
        hook.OnAfter = c => { Assert.IsNull(c.Result); c.SetResult(null); };
        Assert.IsNull(proxy.Run("original"));
    }

    [TestMethod]
    public async Task VoidFailureAndCancellation_CannotBeConvertedIntoSuccess()
    {
        var hook = new Hook { OnAfter = c => c.SetResult("replacement") };
        var proxy = MethodHookProxy<IService>.Create(new Service(), hook);
        proxy.Nothing();
        await proxy.NothingAsync();
        var error = await Assert.ThrowsExceptionAsync<ApplicationException>(() => proxy.FailAsync());
        Assert.AreEqual("business", error.Message);
        Assert.AreEqual(0, error.Data.Count);
        using var source = new CancellationTokenSource(); source.Cancel();
        hook.OnBefore = c => { c.SetArgument("cancellationToken", source.Token); Assert.AreEqual(source.Token, c.CancellationToken); };
        var task = proxy.CancelAsync(default);
        try { await task; Assert.Fail(); } catch (OperationCanceledException) { }
        Assert.IsTrue(task.IsCanceled);
    }

    public interface INamed { Task<string> SayHolle(string msg); }
    public sealed class Named : INamed
    {
        [BeforeMethod(nameof(RecordBefore)), AfterMethod(nameof(RecordAfter))]
        public async Task<string> SayHolle(string msg) { await Task.Yield(); return msg; }
        private void RecordBefore(MethodInvocationContext c) => c.SetArgument("msg", c.GetArgument<string>("msg") + "-before");
        private async Task RecordAfter(MethodInvocationContext c) { await Task.Yield(); c.SetResult(c.Result + "-after"); }
    }

    [TestMethod]
    public async Task NamedHooks_AndConcurrentInvocations_UseTheirOwnArgumentsAndResults()
    {
        var proxy = MethodHookProxy<INamed>.Create(new Named());
        var results = await Task.WhenAll(Enumerable.Range(0, 30).Select(i => proxy.SayHolle(i.ToString())));
        CollectionAssert.AreEqual(Enumerable.Range(0, 30).Select(i => i + "-before-after").ToArray(), results);
    }
}
