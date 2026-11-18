using M.Simple.Caching.MongoDB;
using M.Simple.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;

namespace M.Simple.Caching.Test;

[TestClass]
public sealed partial class CacheContractTests
{
    private readonly List<ServiceProvider> _providers = new();
    private readonly string _prefix = "m-simple-cache-test:" + Guid.NewGuid().ToString("N") + ":";
    private readonly string _database = "m_simple_cache_test_" + Guid.NewGuid().ToString("N");
    private bool _mongoUsed;
    private bool _redisUsed;

    private ISimpleCache Create(string backend, string suffix = "")
    {
        var services = new ServiceCollection();
        void Configure(SimpleCacheOptions options) { options.KeyPrefix = _prefix + suffix; options.FactoryTimeout = TimeSpan.FromSeconds(15); }
        switch (backend)
        {
            case "Memory": services.AddSimpleMemoryCache(o => Configure(o)); break;
            case "Redis":
                var redis = Environment.GetEnvironmentVariable("M_SIMPLE_TEST_REDIS");
                if (string.IsNullOrWhiteSpace(redis)) Assert.Inconclusive("Set M_SIMPLE_TEST_REDIS to enable real Redis integration tests.");
                services.AddSimpleRedisCache(o => { Configure(o); o.ConnectionString = redis!; });
                _redisUsed = true;
                break;
            case "MongoDB":
                var mongo = Environment.GetEnvironmentVariable("M_SIMPLE_TEST_MONGODB");
                if (string.IsNullOrWhiteSpace(mongo)) Assert.Inconclusive("Set M_SIMPLE_TEST_MONGODB to enable real MongoDB integration tests.");
                services.AddSimpleMongoCache(o => { Configure(o); o.ConnectionString = mongo!; o.DatabaseName = _database; });
                _mongoUsed = true;
                break;
            default: throw new ArgumentException(nameof(backend));
        }
        var provider = services.BuildServiceProvider();
        _providers.Add(provider);
        return provider.GetRequiredService<ISimpleCache>();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        foreach (var provider in _providers) await provider.DisposeAsync();
        if (_redisUsed)
        {
            using var redis = await ConnectionMultiplexer.ConnectAsync(Environment.GetEnvironmentVariable("M_SIMPLE_TEST_REDIS")!);
            var db = redis.GetDatabase();
            foreach (var endpoint in redis.GetEndPoints())
            {
                var server = redis.GetServer(endpoint);
                foreach (var key in server.Keys(database: db.Database, pattern: _prefix + "*")) await db.KeyDeleteAsync(key);
            }
        }
        if (_mongoUsed)
        {
            var client = new MongoClient(Environment.GetEnvironmentVariable("M_SIMPLE_TEST_MONGODB"));
            try { await client.DropDatabaseAsync(_database); }
            finally { ((IDisposable)client).Dispose(); }
        }
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task ValuesAreSerializedSnapshotsAndCanBeUpdatedAndRemoved(string backend)
    {
        var cache = Create(backend);
        var value = new Item { Id = 7, Name = "原始", Amount = 123.45m, Age = null };
        await cache.SetAsync("item", value);
        value.Name = "modified";
        var loaded = await cache.TryGetAsync<Item>("item");
        Assert.IsTrue(loaded.Found);
        Assert.AreEqual("原始", loaded.Value!.Name);
        Assert.AreEqual(123.45m, loaded.Value.Amount);
        Assert.IsNull(loaded.Value.Age);
        loaded.Value.Name = "mutated read";
        Assert.AreEqual("原始", (await cache.TryGetAsync<Item>("item")).Value!.Name);
        await cache.SetAsync("item", new Item { Id = 8, Name = "更新" });
        Assert.AreEqual(8, (await cache.TryGetAsync<Item>("item")).Value!.Id);
        await cache.RemoveAsync("item");
        Assert.IsFalse(await cache.ExistsAsync("item"));
        Assert.IsFalse((await cache.TryGetAsync<Item>("item")).Found);
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task AbsoluteExpirationIsApplied(string backend)
    {
        var cache = Create(backend);
        await cache.SetAsync("expires", 1, new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(200) });
        Assert.IsTrue(await cache.ExistsAsync("expires"));
        await Task.Delay(350);
        Assert.IsFalse(await cache.ExistsAsync("expires"));
        Assert.IsFalse((await cache.TryGetAsync<int>("expires")).Found);
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task SlidingTouchesButDoesNotExceedAbsoluteLimit(string backend)
    {
        var cache = Create(backend);
        await cache.SetAsync("sliding", 1, new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2), SlidingExpiration = TimeSpan.FromSeconds(1) });
        for (var i = 0; i < 3; i++)
        {
            await Task.Delay(400);
            Assert.IsTrue((await cache.TryGetAsync<int>("sliding")).Found);
        }
        await Task.Delay(1000);
        Assert.IsFalse((await cache.TryGetAsync<int>("sliding")).Found);
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task ExistsDoesNotTouchSlidingTime(string backend)
    {
        var cache = Create(backend);
        await cache.SetAsync("exists", 1, new() { SlidingExpiration = TimeSpan.FromMilliseconds(500) });
        await Task.Delay(300);
        Assert.IsTrue(await cache.ExistsAsync("exists"));
        await Task.Delay(300);
        Assert.IsFalse(await cache.ExistsAsync("exists"));
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task ConcurrentMissesHaveOneFactoryPerProcess(string backend)
    {
        var cache = Create(backend);
        var calls = 0;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 20).Select(_ => cache.GetOrCreateAsync("concurrent", ct =>
        {
            Interlocked.Increment(ref calls);
            started.TrySetResult();
            return release.Task;
        })).ToArray();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        release.SetResult(42);
        var values = await Task.WhenAll(tasks);
        Assert.IsTrue(values.All(value => value == 42));
        Assert.AreEqual(1, calls);
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task NullCachingIsOptionalAndExplicitNullIsAHit(string backend)
    {
        var cache = Create(backend);
        var calls = 0;
        Task<string?> Factory(CancellationToken _) { calls++; return Task.FromResult<string?>(null); }
        await cache.GetOrCreateAsync("null", Factory);
        await cache.GetOrCreateAsync("null", Factory);
        Assert.AreEqual(2, calls);
        await cache.GetOrCreateAsync("null", Factory, new() { CacheNullValues = true });
        await cache.GetOrCreateAsync("null", Factory, new() { CacheNullValues = true });
        Assert.AreEqual(3, calls);
        Assert.AreEqual(new CacheResult<string?>(true, null), await cache.TryGetAsync<string?>("null"));
        await cache.SetAsync("zero", 0);
        await cache.SetAsync("false", false);
        Assert.AreEqual(new CacheResult<int>(true, 0), await cache.TryGetAsync<int>("zero"));
        Assert.AreEqual(new CacheResult<bool>(true, false), await cache.TryGetAsync<bool>("false"));
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task FactoryExceptionIsNotCachedAndDifferentTypesCannotShareKey(string backend)
    {
        var cache = Create(backend);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            cache.GetOrCreateAsync<int>("failure", _ => Task.FromException<int>(new InvalidOperationException("factory"))));
        Assert.IsFalse(await cache.ExistsAsync("failure"));
        Assert.AreEqual(7, await cache.GetOrCreateAsync("failure", _ => Task.FromResult(7)));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => cache.TryGetAsync<string>("failure"));
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task RemoveDuringFactoryPreventsStaleRepopulation(string backend)
    {
        var cache = Create(backend);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = cache.GetOrCreateAsync("remove", _ => { started.SetResult(); return release.Task; });
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cache.RemoveAsync("remove");
        release.SetResult(1);
        Assert.AreEqual(1, await pending);
        Assert.IsFalse(await cache.ExistsAsync("remove"));
        Assert.AreEqual(2, await cache.GetOrCreateAsync("remove", _ => Task.FromResult(2)));
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task ExplicitSetWinsOverAnOlderFactory(string backend)
    {
        var cache = Create(backend);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = cache.GetOrCreateAsync("set", _ => { started.SetResult(); return release.Task; });
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cache.SetAsync("set", 9);
        release.SetResult(1);
        Assert.AreEqual(1, await pending);
        Assert.AreEqual(9, (await cache.TryGetAsync<int>("set")).Value);
    }

    [DataTestMethod]
    [DataRow("Redis"), DataRow("MongoDB")]
    public async Task SeparateProvidersShareBackendButPrefixesAreIsolated(string backend)
    {
        var first = Create(backend, "one:");
        var second = Create(backend, "one:");
        var isolated = Create(backend, "two:");
        await first.SetAsync("shared", 123, new() { NeverExpires = true });
        Assert.AreEqual(123, (await second.TryGetAsync<int>("shared")).Value);
        Assert.IsFalse(await isolated.ExistsAsync("shared"));
    }

    [TestMethod]
    public async Task MongoFiltersExpiredDocumentEvenWhenTtlIndexHasNotDeletedIt()
    {
        var cache = Create("MongoDB");
        await cache.SetAsync("expired", 1);
        var client = new MongoClient(Environment.GetEnvironmentVariable("M_SIMPLE_TEST_MONGODB"));
        try
        {
            var collection = client.GetDatabase(_database).GetCollection<BsonDocument>("CacheEntries");
            await collection.Indexes.DropOneAsync("M_Simple_Cache_ExpiresAt");
            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", _prefix + "expired"),
                Builders<BsonDocument>.Update.Set("expiresAt", DateTime.UtcNow.AddMinutes(-10)));
            Assert.IsFalse((await cache.TryGetAsync<int>("expired")).Found);
            Assert.IsFalse(await cache.ExistsAsync("expired"));
            Assert.AreEqual(1L, await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty));
        }
        finally { ((IDisposable)client).Dispose(); }
    }

    public sealed class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Amount { get; set; }
        public int? Age { get; set; }
    }
}
