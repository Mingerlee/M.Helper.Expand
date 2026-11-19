using System.Text;
using System.Text.Json;
using M.Simple.Caching.Dashboard;
using M.Simple.Caching.MongoDB;
using M.Simple.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using StackExchange.Redis;

namespace M.Simple.Caching.Test;

[TestClass]
public sealed class NativeCacheReadTests
{
    private readonly string _root = "m-simple-native-read:" + Guid.NewGuid().ToString("N") + ":";
    private readonly string _database = "m_simple_native_read_" + Guid.NewGuid().ToString("N");
    private readonly HashSet<RedisKey> _keys = [];
    private ServiceProvider? _provider;
    private string? _backend;
    private ConnectionMultiplexer? _redis;
    private MongoClient? _mongo;
    private ISimpleNativeCache Native => _provider!.GetRequiredService<ISimpleNativeCache>();

    private async Task Create(string backend, Action<SimpleNativeCacheOptions>? configure = null, CacheDashboardScope dashboardScope = CacheDashboardScope.All)
    {
        var address = Environment.GetEnvironmentVariable(backend == "Redis" ? "M_SIMPLE_TEST_REDIS" : "M_SIMPLE_TEST_MONGODB");
        if (backend != "Memory" && string.IsNullOrWhiteSpace(address)) Assert.Inconclusive("Set the backend test connection environment variable.");
        _backend = backend;
        var services = new ServiceCollection();
        // Configuring native reads before backend registration must preserve the configuration.
        if (configure is not null) services.AddSimpleNativeCache(configure);
        if (backend == "Memory") services.AddSimpleMemoryCache(o => o.KeyPrefix = _root + "app:");
        else if (backend == "Redis")
        {
            services.AddSimpleRedisCache(o => { o.ConnectionString = address!; o.KeyPrefix = _root + "app:"; });
            _redis = await ConnectionMultiplexer.ConnectAsync(address!);
        }
        else
        {
            services.AddSimpleMongoCache(o => { o.ConnectionString = address!; o.DatabaseName = _database; o.KeyPrefix = _root + "app:"; });
            _mongo = new MongoClient(address!);
        }
        services.AddSimpleCacheDashboard(o => o.Scope = dashboardScope);
        _provider = services.BuildServiceProvider();
    }

    private async Task Seed(string key, string name = "native", TimeSpan? expiration = null)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { name });
        if (_backend == "Redis")
        {
            _keys.Add(key);
            await _redis!.GetDatabase().StringSetAsync(key, bytes);
            if (expiration.HasValue) await _redis.GetDatabase().KeyExpireAsync(key, expiration.Value);
        }
        else if (_backend == "MongoDB")
            await _mongo!.GetDatabase(_database).GetCollection<BsonDocument>("CacheEntries")
                .InsertOneAsync(new() { ["_id"] = key, ["name"] = name });
        else await _provider!.GetRequiredService<ISimpleCacheStore>().WriteAsync(key, new("unrelated-type-marker", bytes), new(expiration, null), default);
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task DefaultScopeUsesFullKeysAndIsIndependentFromDashboard(string backend)
    {
        await Create(backend);
        var key = _root + "app:product";
        await Seed(key);
        await Seed(_root + "other:product");
        var admin = _provider!.GetRequiredService<ISimpleCacheAdmin>();
        var stats = admin.GetStatistics();
        Assert.AreEqual(CacheDashboardScope.All, admin.Scope);
        var result = await Native.TryGetAsync<Product>(key);
        Assert.IsTrue(result.Found);
        Assert.AreEqual("native", result.Value!.Name);
        Assert.IsTrue(await Native.ExistsAsync(key));
        Assert.IsFalse((await Native.TryGetAsync<Product>(_root + "app:missing")).Found);
        Assert.IsFalse(await Native.ExistsAsync(_root + "app:missing"));
        Assert.AreEqual(stats, admin.GetStatistics());
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Native.TryGetAsync<Product>("product"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Native.TryGetAsync<Product>(_root + "other:product"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Native.ExistsAsync(_root + "other:product"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Native.HashGetAllAsync(_root + "other:product"));
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => Native.TryGetAsync<Product>(key, canceled.Token));
        if (backend == "MongoDB")
        {
            using var indexes = await _mongo!.GetDatabase(_database).GetCollection<BsonDocument>("CacheEntries").Indexes.ListAsync();
            CollectionAssert.AreEqual(new[] { "_id_" }, (await indexes.ToListAsync()).Select(i => i["name"].AsString).ToArray());
            var document = await Native.TryGetAsync<BsonDocument>(key);
            Assert.AreEqual(key, document.Value!["_id"].AsString);
            var bson = await Native.TryGetAsync<byte[]>(key);
            Assert.IsTrue(bson.Value!.Length > 0);
        }
        else Assert.IsTrue((await Native.TryGetAsync<byte[]>(key)).Value!.Length > 0);
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task PrefixScopeIsLiteralUnionAndSnapshotsCallerConfiguration(string backend)
    {
        var prefixes = new[] { _root + "ids:", _root + "urn:" };
        await Create(backend, o => { o.Scope = NativeCacheScope.Prefixes; o.Prefixes = prefixes; });
        prefixes[0] = _root; // Mutating caller configuration cannot widen access.
        await Seed(_root + "ids:1");
        await Seed(_root + "urn:2");
        await Seed(_root + "outside:3");
        Assert.IsTrue((await Native.TryGetAsync<Product>(_root + "ids:1")).Found);
        Assert.IsTrue((await Native.TryGetAsync<Product>(_root + "urn:2")).Found);
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Native.TryGetAsync<Product>(_root + "outside:3"));
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task AllScopeCanReadOutsideBusinessPrefixButDoesNotChangeBusinessReads(string backend)
    {
        await Create(backend, o => o.Scope = NativeCacheScope.All, CacheDashboardScope.CurrentApplication);
        Assert.AreEqual(CacheDashboardScope.CurrentApplication, _provider!.GetRequiredService<ISimpleCacheAdmin>().Scope);
        var key = _root + "outside:1";
        await Seed(key);
        Assert.AreEqual("native", (await Native.TryGetAsync<Product>(key)).Value!.Name);
        var cache = _provider!.GetRequiredService<ISimpleCache>();
        Assert.IsFalse((await cache.TryGetAsync<Product>(key)).Found); // Business API still prepends its prefix.
        await cache.SetAsync("managed", new Product { Name = "business" });
        Assert.AreEqual("business", (await cache.TryGetAsync<Product>("managed")).Value!.Name);
        if (backend == "Redis")
        {
            _keys.Add(_root + "app:managed");
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Native.TryGetAsync<Product>(_root + "app:managed"));
        }
        else if (backend == "MongoDB")
            Assert.IsTrue((await Native.TryGetAsync<BsonDocument>(_root + "app:managed")).Value!.Contains("payload"));
        else
            await Assert.ThrowsExceptionAsync<NotSupportedException>(() => Native.ListRangeAsync(key));
    }

    [TestMethod]
    public async Task RedisStringsPreserveBytesAndUtf8AndRejectMalformedJsonAndWrongTypes()
    {
        await Create("Redis");
        var db = _redis!.GetDatabase();
        var key = _root + "app:raw";
        _keys.Add(key);
        await db.StringSetAsync(key, "plain text");
        Assert.AreEqual("plain text", (await Native.TryGetAsync<string>(key)).Value);
        await Assert.ThrowsExceptionAsync<JsonException>(() => Native.TryGetAsync<Product>(key));
        await db.StringSetAsync(key, new byte[] { 0, 255, 42 });
        CollectionAssert.AreEqual(new byte[] { 0, 255, 42 }, (await Native.TryGetAsync<byte[]>(key)).Value);
        await Assert.ThrowsExceptionAsync<DecoderFallbackException>(() => Native.TryGetAsync<string>(key));
        await db.StringSetAsync(key, "null");
        var nil = await Native.TryGetAsync<Product>(key);
        Assert.IsTrue(nil.Found);
        Assert.IsNull(nil.Value);
        Assert.AreEqual("null", (await Native.TryGetAsync<string>(key)).Value);
        await db.KeyDeleteAsync(key);
        await db.HashSetAsync(key, "field", "value");
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Native.TryGetAsync<string>(key));
        Assert.IsTrue(await Native.ExistsAsync(key));
    }

    [TestMethod]
    public async Task RedisCollectionsReturnExactBoundedResultsWithoutChangingTtl()
    {
        await Create("Redis", o => { o.MaxCollectionItems = 3; o.MaxPayloadBytes = 40; });
        var db = _redis!.GetDatabase();
        var hash = _root + "app:hash";
        var list = _root + "app:list";
        var set = _root + "app:set";
        var sorted = _root + "app:sorted";
        foreach (var key in new[] { hash, list, set, sorted }) _keys.Add(key);
        await db.HashSetAsync(hash, [new("name", "中文"), new("binary", new byte[] { 255, 0 })]);
        await db.ListRightPushAsync(list, ["one", "two", "three", "four"]);
        await db.SetAddAsync(set, ["one", "two"]);
        await db.SortedSetAddAsync(sorted, [new("low", 1.5), new("high", 8.25)]);
        await db.KeyExpireAsync(hash, TimeSpan.FromMinutes(5));
        var ttl = await db.KeyTimeToLiveAsync(hash);
        var fields = await Native.HashGetAllAsync(hash);
        Assert.AreEqual("中文", Encoding.UTF8.GetString(fields.Value!["name"]));
        CollectionAssert.AreEqual(new byte[] { 255, 0 }, fields.Value["binary"]);
        var range = await Native.ListRangeAsync(list, 1, 2);
        CollectionAssert.AreEqual(new[] { "two", "three" }, range.Value!.Select(bytes => Encoding.UTF8.GetString(bytes)).ToArray());
        var beyond = await Native.ListRangeAsync(list, 20, 2);
        Assert.IsTrue(beyond.Found);
        Assert.AreEqual(0, beyond.Value!.Count);
        CollectionAssert.AreEquivalent(new[] { "one", "two" }, (await Native.SetMembersAsync(set)).Value!.Select(bytes => Encoding.UTF8.GetString(bytes)).ToArray());
        var scores = await Native.SortedSetRangeAsync(sorted, 0, 2);
        Assert.AreEqual(1.5, scores.Value![0].Score);
        Assert.AreEqual("high", Encoding.UTF8.GetString(scores.Value[1].Member));
        Assert.IsTrue((await db.KeyTimeToLiveAsync(hash)) <= ttl);
        Assert.IsFalse((await Native.HashGetAllAsync(_root + "app:missing")).Found);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Native.ListRangeAsync(hash, 0, 1));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Native.ListRangeAsync(list, -1, 1));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Native.ListRangeAsync(list, 0, 4));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Native.SortedSetRangeAsync(sorted, long.MaxValue, 1));
        await db.SetAddAsync(set, ["three", "four"]);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Native.SetMembersAsync(set));
        await db.HashSetAsync(hash, [new("third", "3"), new("fourth", "4")]);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Native.HashGetAllAsync(hash));
        await db.HashDeleteAsync(hash, ["third", "fourth"]);
        await db.HashSetAsync(hash, "name", new string('x', 100));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Native.HashGetAllAsync(hash));
        await db.ListRightPushAsync(list, new string('x', 100));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Native.ListRangeAsync(list, 4, 1));
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task PayloadLimitsFailInsteadOfReturningMissOrPartialData(string backend)
    {
        await Create(backend, o => o.MaxPayloadBytes = 32);
        var key = _root + "app:large";
        await Seed(key, new string('x', 100));
        Assert.IsTrue(await Native.ExistsAsync(key));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Native.TryGetAsync<Product>(key));
    }

    [TestMethod]
    public async Task MemoryNativeReadsDoNotRenewSlidingExpiration()
    {
        await Create("Memory");
        var key = _root + "app:sliding";
        await _provider!.GetRequiredService<ISimpleCacheStore>().WriteAsync(key,
            new("irrelevant", Encoding.UTF8.GetBytes("42")), new(null, TimeSpan.FromMilliseconds(500)), default);
        await Task.Delay(200);
        Assert.AreEqual(42, (await Native.TryGetAsync<int>(key)).Value);
        await Task.Delay(400);
        Assert.IsFalse((await Native.TryGetAsync<int>(key)).Found);
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis")]
    public async Task NativeReadsRespectBackendExpiryWithoutTouchingIt(string backend)
    {
        await Create(backend);
        var key = _root + "app:expires";
        await Seed(key, expiration: TimeSpan.FromMilliseconds(500));
        await Task.Delay(200);
        Assert.IsTrue((await Native.TryGetAsync<Product>(key)).Found);
        await Task.Delay(400);
        Assert.IsFalse(await Native.ExistsAsync(key));
        Assert.IsFalse((await Native.TryGetAsync<Product>(key)).Found);
    }

    [TestMethod]
    public async Task MongoReadsStringIdLiterallyAndPropagatesBsonTypeMismatch()
    {
        await Create("MongoDB", o => o.Scope = NativeCacheScope.All);
        var collection = _mongo!.GetDatabase(_database).GetCollection<BsonDocument>("CacheEntries");
        var id = ObjectId.GenerateNewId();
        await collection.InsertManyAsync(new BsonDocument[]
        {
            new() { ["_id"] = id, ["name"] = "object-id-document" },
            new() { ["_id"] = id.ToString(), ["name"] = "string-id-document" }
        });
        Assert.AreEqual("string-id-document", (await Native.TryGetAsync<Product>(id.ToString())).Value!.Name);
        await Assert.ThrowsExceptionAsync<FormatException>(() => Native.TryGetAsync<WrongMongoType>(id.ToString()));
        await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id.ToString()));
        Assert.IsFalse(await Native.ExistsAsync(id.ToString()));
        Assert.IsFalse((await Native.TryGetAsync<Product>(id.ToString())).Found);
    }

    [TestMethod]
    public void InvalidOptionsFailAtRegistrationAndEmptyDefaultPrefixFailsClosed()
    {
        foreach (var configure in new Action<SimpleNativeCacheOptions>[]
        {
            o => o.Scope = (NativeCacheScope)99,
            o => o.Scope = NativeCacheScope.Prefixes,
            o => { o.Scope = NativeCacheScope.Prefixes; o.Prefixes = [" "]; },
            o => o.Prefixes = ["unexpected:"],
            o => o.MaxPayloadBytes = 0,
            o => o.MaxCollectionItems = 0,
            o => o.SerializerOptions = null!
        }) Assert.ThrowsException<ArgumentException>(() => new ServiceCollection().AddSimpleNativeCache(configure));
        using var provider = new ServiceCollection().AddSimpleMemoryCache().BuildServiceProvider();
        Assert.ThrowsException<InvalidOperationException>(() => provider.GetRequiredService<ISimpleNativeCache>());
        var services = new ServiceCollection().AddSimpleMemoryCache();
        services.AddSimpleNativeCache(o => o.Scope = NativeCacheScope.All);
        using var all = services.BuildServiceProvider();
        Assert.IsNotNull(all.GetRequiredService<ISimpleNativeCache>());
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_provider is not null) await _provider.DisposeAsync();
        if (_redis is not null)
        {
            foreach (var key in _keys) await _redis.GetDatabase().KeyDeleteAsync(key);
            _redis.Dispose();
        }
        if (_mongo is not null)
        {
            await _mongo.DropDatabaseAsync(_database);
            _mongo.Dispose();
        }
    }

    [BsonIgnoreExtraElements]
    public sealed class Product
    {
        [BsonElement("name")]
        public string Name { get; set; } = "";
    }

    [BsonIgnoreExtraElements]
    public sealed class WrongMongoType
    {
        [BsonElement("name")]
        public int Name { get; set; }
    }
}
