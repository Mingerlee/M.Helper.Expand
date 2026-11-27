using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Services.Interception.Test;

[TestClass]
public sealed class FailureOptionsTests
{
    public interface IService
    {
        int StopBefore();
        Task<int> StopBeforeAsync();
        int StopAfter();
        ValueTask<int> StopAfterAsync();
        int Fail();
        Task<int> FailAsync();
    }
    public sealed class Service : IService
    {
        public int BusinessCalls;
        public int AfterCalls;
        public Exception BusinessError = new InvalidOperationException("business");
        public Exception HookError = new ApplicationException("hook");
        [BeforeMethod(nameof(Before), ContinueOnFailure = false)]
        public int StopBefore() { BusinessCalls++; return 1; }
        [BeforeMethod(nameof(BeforeAsync), ContinueOnFailure = false)]
        public Task<int> StopBeforeAsync() { BusinessCalls++; return Task.FromResult(1); }
        [AfterMethod(nameof(After), ContinueOnFailure = false)]
        public int StopAfter() => 1;
        [AfterMethod(nameof(AfterAsync), ContinueOnFailure = false)]
        public ValueTask<int> StopAfterAsync() => ValueTask.FromResult(1);
        [AfterMethod(nameof(After), RunOnBusinessFailure = true, ContinueOnFailure = false)]
        public int Fail() => throw BusinessError;
        [AfterMethod(nameof(AfterAsync), RunOnBusinessFailure = true, ContinueOnFailure = false)]
        public Task<int> FailAsync() => Task.FromException<int>(BusinessError);
        private void Before(MethodInvocationContext context) => throw HookError;
        private async Task BeforeAsync(MethodInvocationContext context) { await Task.Yield(); throw HookError; }
        private void After(MethodInvocationContext context) { AfterCalls++; throw HookError; }
        private async ValueTask AfterAsync(MethodInvocationContext context) { await Task.Yield(); After(context); }
    }

    [TestMethod]
    public async Task ExplicitFalsePropagatesHookFailureAndStopsBeforeBusiness()
    {
        var target = new Service(); var proxy = MethodHookProxy<IService>.Create(target);
        Assert.AreSame(target.HookError, Assert.ThrowsException<ApplicationException>(() => proxy.StopBefore()));
        Assert.AreSame(target.HookError, await Assert.ThrowsExceptionAsync<ApplicationException>(() => proxy.StopBeforeAsync()));
        Assert.AreEqual(0, target.BusinessCalls);
        Assert.AreSame(target.HookError, Assert.ThrowsException<ApplicationException>(() => proxy.StopAfter()));
        Assert.AreSame(target.HookError, await Assert.ThrowsExceptionAsync<ApplicationException>(async () => await proxy.StopAfterAsync()));
    }

    [TestMethod]
    public async Task OptInAfterRunsOnBusinessFailureAndPreservesOriginalError()
    {
        var target = new Service(); var proxy = MethodHookProxy<IService>.Create(target);
        Assert.AreSame(target.BusinessError, Assert.ThrowsException<InvalidOperationException>(() => proxy.Fail()));
        Assert.AreSame(target.BusinessError, await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => proxy.FailAsync()));
        Assert.AreEqual(2, target.AfterCalls);
    }
}
