using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Caching.Test;

[TestClass]
public sealed class MemoryCacheTests
{
    private static ServiceProvider Create(Action<SimpleMemoryCacheOptions>? configure = null, TimeProvider? clock = null)
    {
        var services = new ServiceCollection();
        if (clock is not null) services.AddSingleton(clock);
        services.AddSimpleMemoryCache(configure);
        return services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task MissIsDifferentFromCachedNullZeroAndFalse()
    {
        using var services = Create();
        var cache = services.GetRequiredService<ISimpleCache>();
        Assert.IsFalse((await cache.TryGetAsync<int>("number")).Found);
        await cache.SetAsync("number", 0);
        await cache.SetAsync("bool", false);
        await cache.SetAsync<string?>("null", null);
        Assert.AreEqual(new CacheResult<int>(true, 0), await cache.TryGetAsync<int>("number"));
        Assert.AreEqual(new CacheResult<bool>(true, false), await cache.TryGetAsync<bool>("bool"));
        Assert.AreEqual(new CacheResult<string?>(true, null), await cache.TryGetAsync<string?>("null"));
    }

    [TestMethod]
    public async Task SlidingExpirationCannotExtendBeyondAbsoluteCap()
    {
        var clock = new ManualClock();
        using var services = Create(clock: clock);
        var cache = services.GetRequiredService<ISimpleCache>();
        await cache.SetAsync("key", 1, new() { SlidingExpiration = TimeSpan.FromMinutes(2), AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        for (var i = 0; i < 4; i++)
        {
            clock.Advance(TimeSpan.FromMinutes(1));
            Assert.IsTrue((await cache.TryGetAsync<int>("key")).Found);
        }
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.IsFalse((await cache.TryGetAsync<int>("key")).Found);
    }

    [TestMethod]
    public async Task ExistsDoesNotRefreshSlidingExpiration()
    {
        var clock = new ManualClock();
        using var services = Create(clock: clock);
        var cache = services.GetRequiredService<ISimpleCache>();
        await cache.SetAsync("key", 1, new() { SlidingExpiration = TimeSpan.FromMinutes(2) });
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.IsTrue(await cache.ExistsAsync("key"));
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.IsFalse(await cache.ExistsAsync("key"));
    }

    [TestMethod]
    public async Task NeverExpiresAndDefaultExpirationAreDistinct()
    {
        var clock = new ManualClock();
        using var services = Create(clock: clock);
        var cache = services.GetRequiredService<ISimpleCache>();
        await cache.SetAsync("default", 1);
        await cache.SetAsync("forever", 2, new() { NeverExpires = true });
        clock.Advance(TimeSpan.FromDays(1));
        Assert.IsFalse(await cache.ExistsAsync("default"));
        Assert.IsTrue(await cache.ExistsAsync("forever"));
    }

    [TestMethod]
    public async Task NullResultsUseShortTtlEvenWithNeverExpires()
    {
        var clock = new ManualClock();
        using var services = Create(o => { o.CacheNullValues = true; o.NullExpiration = TimeSpan.FromMinutes(1); }, clock);
        var cache = services.GetRequiredService<ISimpleCache>();
        await cache.GetOrCreateAsync<string?>("null", _ => Task.FromResult<string?>(null), new() { NeverExpires = true });
        Assert.IsTrue((await cache.TryGetAsync<string?>("null")).Found);
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.IsFalse(await cache.ExistsAsync("null"));
    }

    [TestMethod]
    public async Task CancelledWaiterDoesNotCancelSharedFactory()
    {
        using var services = Create();
        var cache = services.GetRequiredService<ISimpleCache>();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var factoryToken = CancellationToken.None;
        using var caller = new CancellationTokenSource();
        var first = cache.GetOrCreateAsync("key", ct => { factoryToken = ct; started.SetResult(); return release.Task; }, cancellationToken: caller.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = cache.GetOrCreateAsync<int>("key", _ => throw new AssertFailedException("Duplicate factory"));
        caller.Cancel();
        await Cancelled(first);
        Assert.IsFalse(factoryToken.IsCancellationRequested);
        release.SetResult(42);
        Assert.AreEqual(42, await second);
        Assert.AreEqual(42, (await cache.TryGetAsync<int>("key")).Value);
    }

    [TestMethod]
    public async Task FactoryTimeoutDoesNotPoisonKeyOrCacheLateCompletion()
    {
        using var services = Create(o => o.FactoryTimeout = TimeSpan.FromMilliseconds(100));
        var cache = services.GetRequiredService<ISimpleCache>();
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Cancelled(cache.GetOrCreateAsync("key", _ => release.Task));
        Assert.AreEqual(2, await cache.GetOrCreateAsync("key", _ => Task.FromResult(2)));
        release.SetResult(1);
        Assert.AreEqual(2, (await cache.TryGetAsync<int>("key")).Value);
    }

    [TestMethod]
    public async Task EntryAndPayloadLimitsAreEnforced()
    {
        using var services = Create(o => { o.MaxEntryCount = 2; o.MaxPayloadBytes = 128; });
        var cache = services.GetRequiredService<ISimpleCache>();
        for (var i = 0; i < 10; i++) await cache.SetAsync("key" + i, i);
        var retained = 0;
        for (var i = 0; i < 10; i++) if (await cache.ExistsAsync("key" + i)) retained++;
        Assert.IsTrue(retained <= 2);
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => cache.SetAsync("large", new string('x', 200)));
        Assert.IsFalse(await cache.ExistsAsync("large"));
    }

    [TestMethod]
    public async Task MemoryByteBudgetCanRejectOversizedEntry()
    {
        using var services = Create(o => o.MaxSizeBytes = 256);
        var cache = services.GetRequiredService<ISimpleCache>();
        await cache.SetAsync("large", new string('x', 300));
        Assert.IsFalse(await cache.ExistsAsync("large"));
    }

    [TestMethod]
    public void InvalidOptionsAndDuplicateBackendFailDuringRegistration()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ServiceCollection().AddSimpleMemoryCache(o => o.MaxEntryCount = 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ServiceCollection().AddSimpleMemoryCache(o => o.DefaultExpiration = TimeSpan.Zero));
        var services = new ServiceCollection();
        services.AddSimpleMemoryCache();
        Assert.ThrowsException<InvalidOperationException>(() => services.AddSimpleMemoryCache());
    }

    [TestMethod]
    public async Task InvalidEntryPolicyAndKeyFailBeforeWrite()
    {
        using var services = Create();
        var cache = services.GetRequiredService<ISimpleCache>();
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => cache.SetAsync("", 1));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => cache.SetAsync("key", 1,
            new() { NeverExpires = true, SlidingExpiration = TimeSpan.FromMinutes(1) }));
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => cache.SetAsync("key", 1,
            new() { SlidingExpiration = TimeSpan.FromMilliseconds(-1) }));
    }

    private static async Task Cancelled(Task task)
    {
        try { await task; Assert.Fail("Expected cancellation."); }
        catch (OperationCanceledException) { }
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        internal void Advance(TimeSpan amount) => _now += amount;
    }
}
