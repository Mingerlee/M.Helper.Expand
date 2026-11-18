using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace M.Simple.Caching.Test;

public sealed partial class CacheContractTests
{
    private ISimpleCacheAdmin Admin => _providers[^1].GetRequiredService<ISimpleCacheAdmin>();

    private static async Task<List<CacheEntryInfo>> All(ISimpleCacheAdmin admin, string prefix, int pageSize = 2)
    {
        var items = new List<CacheEntryInfo>();
        string? cursor = null;
        var pages = 0;
        do
        {
            var page = await admin.ScanAsync(prefix, cursor, pageSize);
            items.AddRange(page.Items);
            cursor = page.NextCursor;
            Assert.IsTrue(++pages < 500, "Scan failed to finish.");
        } while (cursor is not null);
        return items.DistinctBy(i => i.Key).ToList();
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task AdminPaginationAndLiteralPrefixesAreScoped(string backend)
    {
        var cache = Create(backend);
        var admin = Admin;
        for (var i = 0; i < 7; i++) await cache.SetAsync("product:" + i, new { Name = "商品", Id = i });
        await cache.SetAsync("other:1", false);
        await cache.SetAsync("literal:*?[x]\\:", 42);
        await cache.SetAsync("literal:unexpected", 43);
        var all = await All(admin, "product:");
        Assert.AreEqual(7, all.Count);
        Assert.IsTrue(all.All(i => i.Key.StartsWith("product:") && i.SizeBytes > 0));
        var literal = await All(admin, "literal:*?[x]\\:");
        Assert.AreEqual(1, literal.Count);
        Assert.AreEqual("literal:*?[x]\\:", literal[0].Key);
        var detail = await admin.PeekAsync("product:1");
        Assert.IsNotNull(detail);
        StringAssert.Contains(detail.Json, "1");
        Assert.IsTrue(detail.Entry.AbsoluteExpiresAt.HasValue);
        Assert.AreEqual(backend, admin.Backend);

        // A caller cannot use a full key to escape the configured prefix.
        Assert.IsNull(await admin.PeekAsync(_prefix + "product:1"));
        Assert.IsFalse(await admin.RemoveAsync(_prefix + "product:1"));
        Assert.IsTrue(await cache.ExistsAsync("product:1"));
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task AdminReadsNeitherTouchSlidingExpiryNorChangeStatistics(string backend)
    {
        var cache = Create(backend);
        var admin = Admin;
        await cache.SetAsync("sliding", "secret-value", new() { SlidingExpiration = TimeSpan.FromMilliseconds(900) });
        var first = await admin.PeekAsync("sliding");
        var stats = admin.GetStatistics();
        for (var i = 0; i < 3; i++)
        {
            await Task.Delay(150);
            var detail = await admin.PeekAsync("sliding");
            Assert.IsNotNull(detail);
            Assert.AreEqual(first!.Entry.ExpiresAt, detail.Entry.ExpiresAt);
            await All(admin, "sliding");
        }
        Assert.AreEqual(stats, admin.GetStatistics());
        await Task.Delay(600);
        Assert.IsNull(await admin.PeekAsync("sliding"));
        Assert.AreEqual(0, (await All(admin, "sliding")).Count);
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task AdminDeletesInvalidatePendingFillAndReturnActualRemoval(string backend)
    {
        var cache = Create(backend);
        var admin = Admin;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = cache.GetOrCreateAsync("pending", _ => { started.SetResult(); return release.Task; });
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsFalse(await admin.RemoveAsync("pending"));
        release.SetResult(1);
        Assert.AreEqual(1, await pending);
        Assert.IsFalse(await cache.ExistsAsync("pending"));
        await cache.SetAsync("item", 0);
        Assert.IsTrue(await admin.RemoveAsync("item"));
        Assert.IsFalse(await admin.RemoveAsync("item"));
        Assert.IsNull(await admin.PeekAsync("item"));
    }

    [TestMethod]
    public async Task MongoAdminExcludesExpiredDocumentsBeforeTtlCleanup()
    {
        var cache = Create("MongoDB");
        await cache.SetAsync("expired-admin", 1);
        using var client = new MongoClient(Environment.GetEnvironmentVariable("M_SIMPLE_TEST_MONGODB"));
        var collection = client.GetDatabase(_database).GetCollection<BsonDocument>("CacheEntries");
        await collection.Indexes.DropOneAsync("M_Simple_Cache_ExpiresAt");
        await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", _prefix + "expired-admin"),
            Builders<BsonDocument>.Update.Set("expiresAt", DateTime.UtcNow.AddMinutes(-10)));
        Assert.IsNull(await Admin.PeekAsync("expired-admin"));
        Assert.AreEqual(0, (await All(Admin, "")).Count);
        Assert.AreEqual(1L, await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty));
    }
}

[TestClass]
public sealed class CacheAdminMemoryTests
{
    [TestMethod]
    public void DashboardManagementRequiresExplicitPrefix()
    {
        var services = new ServiceCollection().AddSimpleMemoryCache();
        using var provider = services.BuildServiceProvider();
        Assert.ThrowsException<InvalidOperationException>(() => provider.GetRequiredService<ISimpleCacheAdmin>());
        Assert.IsNotNull(provider.GetRequiredService<ISimpleCache>());
    }

    [TestMethod]
    public async Task StatisticsCountPublicLookupsAndSharedLoadsOnly()
    {
        var services = new ServiceCollection().AddSimpleMemoryCache(o => o.KeyPrefix = "stats:");
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ISimpleCache>();
        var admin = provider.GetRequiredService<ISimpleCacheAdmin>();
        await cache.TryGetAsync<int>("missing");
        await cache.SetAsync("zero", 0);
        await cache.TryGetAsync<int>("zero");
        await cache.GetOrCreateAsync("created", _ => Task.FromResult(1));
        await cache.GetOrCreateAsync<int>("created", _ => throw new AssertFailedException());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => cache.GetOrCreateAsync<int>("failure", _ => throw new InvalidOperationException()));
        await admin.PeekAsync("zero");
        await admin.ScanAsync();
        var stats = admin.GetStatistics();
        Assert.AreEqual(2L, stats.Hits);
        Assert.AreEqual(3L, stats.Misses);
        Assert.AreEqual(2L, stats.Loads);
        Assert.AreEqual(1L, stats.LoadFailures);
        Assert.AreEqual(2L, stats.Writes);
    }

    [TestMethod]
    public async Task InvalidCursorAndQueryFailWithoutDeletingAnything()
    {
        var services = new ServiceCollection().AddSimpleMemoryCache(o => o.KeyPrefix = "scope:");
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ISimpleCache>();
        var admin = provider.GetRequiredService<ISimpleCacheAdmin>();
        await cache.SetAsync("a1", 1);
        await cache.SetAsync("a2", 2);
        var page = await admin.ScanAsync("a", pageSize: 1);
        Assert.IsNotNull(page.NextCursor);
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => admin.ScanAsync("other", page.NextCursor));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => admin.ScanAsync(cursor: "tampered!"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => admin.ScanAsync(pageSize: 201));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => admin.RemoveAsync(" "));
        Assert.IsTrue(await cache.ExistsAsync("a1"));
    }

    [TestMethod]
    public async Task EvictionCallbacksDoNotLoseReplacedOrRefreshedEntries()
    {
        var services = new ServiceCollection().AddSimpleMemoryCache(o => { o.KeyPrefix = "index:"; o.MaxEntryCount = 10; });
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ISimpleCache>();
        var admin = provider.GetRequiredService<ISimpleCacheAdmin>();
        for (var i = 0; i < 100; i++)
        {
            await cache.SetAsync("replace", i, new() { SlidingExpiration = TimeSpan.FromMinutes(2) });
            await cache.TryGetAsync<int>("replace");
        }
        await Task.Delay(100);
        Assert.AreEqual(99, (await cache.TryGetAsync<int>("replace")).Value);
        Assert.AreEqual(1, (await admin.ScanAsync("replace")).Items.Count);
        for (var i = 0; i < 100; i++) await cache.SetAsync("capacity:" + i, i);
        await Task.Delay(100);
        var page = await admin.ScanAsync();
        Assert.IsTrue(page.Items.Count <= 10);
        foreach (var item in page.Items) Assert.IsTrue(await cache.ExistsAsync(item.Key));
    }
}
