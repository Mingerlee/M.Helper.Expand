using System.Text;
using M.Simple.Caching.Dashboard;
using M.Simple.Caching.MongoDB;
using M.Simple.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;

namespace M.Simple.Caching.Test;

[TestClass]
public sealed class ManagementScopeTests
{
    private readonly string _root = "m-simple-scope:" + Guid.NewGuid().ToString("N") + ":";
    private readonly string _database = "m_simple_scope_" + Guid.NewGuid().ToString("N");
    private readonly List<ServiceProvider> _providers = [];
    private readonly HashSet<string> _keys = [];
    private string? _backend;

    private ServiceProvider Create(string backend, CacheDashboardScope scope, string[]? prefixes = null)
    {
        var services = new ServiceCollection();
        var redis = Environment.GetEnvironmentVariable("M_SIMPLE_TEST_REDIS");
        var mongo = Environment.GetEnvironmentVariable("M_SIMPLE_TEST_MONGODB");
        if (backend == "Redis" && string.IsNullOrWhiteSpace(redis) || backend == "MongoDB" && string.IsNullOrWhiteSpace(mongo))
            Assert.Inconclusive("Set the backend connection environment variable for integration tests.");
        _backend = backend;
        switch (backend)
        {
            case "Memory": services.AddSimpleMemoryCache(o => o.KeyPrefix = _root + "App:"); break;
            case "Redis": services.AddSimpleRedisCache(o => { o.ConnectionString = redis!; o.KeyPrefix = _root + "App:"; }); break;
            case "MongoDB": services.AddSimpleMongoCache(o => { o.ConnectionString = mongo!; o.DatabaseName = _database; o.KeyPrefix = _root + "App:"; }); break;
            default: throw new ArgumentException(nameof(backend));
        }
        services.AddSimpleCacheDashboard(o => { o.Scope = scope; o.Prefixes = prefixes ?? []; });
        var provider = services.BuildServiceProvider();
        _providers.Add(provider);
        return provider;
    }

    private async Task Write(ServiceProvider provider, string suffix, TimeSpan? ttl = null)
    {
        _keys.Add(_root + suffix);
        await provider.GetRequiredService<ISimpleCacheStore>().WriteAsync(_root + suffix,
            new("test:string", Encoding.UTF8.GetBytes("\"scope-value\"")), new(ttl, null), default);
    }

    private static async Task<List<CacheEntryInfo>> Scan(ISimpleCacheAdmin admin, string prefix = "")
    {
        var result = new List<CacheEntryInfo>();
        string? cursor = null;
        var pages = 0;
        do
        {
            var page = await admin.ScanAsync(prefix, cursor, 1);
            result.AddRange(page.Items);
            cursor = page.NextCursor;
            Assert.IsTrue(++pages < 1000, "Cursor did not terminate.");
        } while (cursor is not null);
        return result;
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task DefaultScopeKeepsRelativeKeysAndHidesOtherApplications(string backend)
    {
        var provider = Create(backend, CacheDashboardScope.CurrentApplication);
        await Write(provider, "App:one");
        await Write(provider, "Other:one");
        var admin = provider.GetRequiredService<ISimpleCacheAdmin>();
        CollectionAssert.AreEqual(new[] { "one" }, (await Scan(admin)).Select(i => i.Key).ToArray());
        Assert.IsNull(await admin.PeekAsync(_root + "Other:one"));
        Assert.IsFalse(await admin.RemoveAsync(_root + "Other:one"));
        Assert.IsTrue(await admin.RemoveAsync("one"));
        Assert.IsNotNull(await ((ISimpleCacheAdminStore)provider.GetRequiredService<ISimpleCacheStore>()).PeekAsync(_root + "Other:one", default));
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task PrefixScopeUnionsDisjointPartitionsAndRejectsOutOfScopeOperations(string backend)
    {
        var prefixes = new[] { _root + "B:", _root + "A:child:", _root + "A:", _root + "A:", _root + "literal:*?[x]\\:" };
        var provider = Create(backend, CacheDashboardScope.Prefixes, prefixes);
        prefixes[0] = _root; // Registration snapshots the mutable caller array.
        foreach (var suffix in new[] { "A:1", "A:child:2", "B:1", "C:1", "literal:*?[x]\\:1", "literal:unexpected" })
            await Write(provider, suffix);
        var admin = provider.GetRequiredService<ISimpleCacheAdmin>();
        var all = await Scan(admin);
        Assert.AreEqual(4, all.Count);
        Assert.AreEqual(4, all.Select(i => i.Key).Distinct().Count());
        Assert.AreEqual(3, admin.Prefixes.Count);
        Assert.AreEqual(4, (await Scan(admin, _root)).Count);
        Assert.AreEqual(1, (await Scan(admin, _root + "A:child:")).Count);
        Assert.AreEqual(1, (await Scan(admin, _root + "literal:*?[x]\\:")).Count);
        Assert.AreEqual(0, (await Scan(admin, _root + "C:")).Count);
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => admin.PeekAsync(_root + "C:1"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => admin.RemoveAsync(_root + "C:1"));
        var detail = await admin.PeekAsync(_root + "B:1");
        Assert.AreEqual(_root + "B:1", detail!.Entry.Key);
        Assert.IsTrue(await admin.RemoveAsync(_root + "B:1"));
        var store = (ISimpleCacheAdminStore)provider.GetRequiredService<ISimpleCacheStore>();
        Assert.IsNotNull(await store.PeekAsync(_root + "C:1", default));
        var page = await admin.ScanAsync(pageSize: 1);
        Assert.IsNotNull(page.NextCursor);
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => admin.ScanAsync(_root + "B:", page.NextCursor));
        var other = Create(backend, CacheDashboardScope.Prefixes, [_root + "A:"]).GetRequiredService<ISimpleCacheAdmin>();
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => other.ScanAsync(cursor: page.NextCursor));
    }

    [DataTestMethod]
    [DataRow("Memory"), DataRow("Redis"), DataRow("MongoDB")]
    public async Task AllScopeUsesFullKeysAndExcludesExpiredEntriesWithoutRenewal(string backend)
    {
        var provider = Create(backend, CacheDashboardScope.AllManaged);
        await Write(provider, "App:one");
        await Write(provider, "Other:one");
        await Write(provider, "expired", TimeSpan.FromMilliseconds(100));
        await Task.Delay(200);
        var admin = provider.GetRequiredService<ISimpleCacheAdmin>();
        if (backend == "Redis")
        {
            using var connection = await ConnectionMultiplexer.ConnectAsync(Environment.GetEnvironmentVariable("M_SIMPLE_TEST_REDIS")!);
            var db = connection.GetDatabase();
            _keys.Add(_root + "foreign-string");
            _keys.Add(_root + "foreign-hash");
            await db.StringSetAsync(_root + "foreign-string", "ordinary business data");
            await db.HashSetAsync(_root + "foreign-hash", new[] { new HashEntry("type", "business"), new HashEntry("payload", "42") });
            Assert.IsFalse(await admin.RemoveAsync(_root + "foreign-string"));
            Assert.IsFalse(await admin.RemoveAsync(_root + "foreign-hash"));
            Assert.IsTrue(await db.KeyExistsAsync(_root + "foreign-hash"));
        }
        if (backend == "MongoDB")
        {
            using var client = new MongoClient(Environment.GetEnvironmentVariable("M_SIMPLE_TEST_MONGODB"));
            await client.GetDatabase(_database).GetCollection<BsonDocument>("CacheEntries")
                .InsertOneAsync(new() { ["_id"] = _root + "foreign-document", ["name"] = "ordinary business data" });
            Assert.IsFalse(await admin.RemoveAsync(_root + "foreign-document"));
        }
        var items = await Scan(admin, _root);
        CollectionAssert.AreEquivalent(new[] { _root + "App:one", _root + "Other:one" }, items.Select(i => i.Key).ToArray());
        // Empty search must include keys outside this instance's business prefix.
        Assert.IsTrue((await Scan(admin)).Any(i => i.Key == _root + "Other:one"));
        var detail = await admin.PeekAsync(_root + "Other:one");
        Assert.AreEqual(_root + "Other:one", detail!.Entry.Key);
        Assert.IsTrue(await admin.RemoveAsync(_root + "Other:one"));
        Assert.IsNull(await admin.PeekAsync(_root + "Other:one"));
        var cache = provider.GetRequiredService<ISimpleCache>();
        _keys.Add(_root + "App:pending");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fill = cache.GetOrCreateAsync("pending", _ => { started.SetResult(); return release.Task; });
        await started.Task;
        await admin.RemoveAsync(_root + "App:pending");
        release.SetResult(7);
        Assert.AreEqual(7, await fill);
        Assert.IsFalse(await cache.ExistsAsync("pending"));
    }

    [TestMethod]
    public void InvalidConfigurationFailsDuringRegistrationAndGlobalScopeAllowsEmptyBusinessPrefix()
    {
        foreach (var options in new Action<SimpleCacheDashboardOptions>[]
        {
            o => o.Scope = CacheDashboardScope.Prefixes,
            o => { o.Scope = CacheDashboardScope.Prefixes; o.Prefixes = [" "]; },
            o => { o.Scope = CacheDashboardScope.Prefixes; o.Prefixes = null!; },
            o => { o.Scope = CacheDashboardScope.Prefixes; o.Prefixes = [new string('x', 513)]; },
            o => { o.Scope = CacheDashboardScope.Prefixes; o.Prefixes = Enumerable.Range(0, 65).Select(i => i + ":").ToArray(); },
            o => { o.Scope = CacheDashboardScope.All; o.Prefixes = ["unexpected:"]; },
            o => o.Scope = (CacheDashboardScope)99
        }) Assert.ThrowsException<ArgumentException>(() => new ServiceCollection().AddSimpleCacheDashboard(options));
        var services = new ServiceCollection();
        services.AddSimpleCacheDashboard(o => o.Scope = CacheDashboardScope.All);
        services.AddSimpleMemoryCache(); // Either registration order must preserve the configured scope.
        using var provider = services.BuildServiceProvider();
        Assert.AreEqual(CacheDashboardScope.All, provider.GetRequiredService<ISimpleCacheAdmin>().Scope);
    }

    [DataTestMethod]
    [DataRow("Redis", CacheDashboardScope.All), DataRow("Redis", CacheDashboardScope.Prefixes)]
    [DataRow("MongoDB", CacheDashboardScope.All), DataRow("MongoDB", CacheDashboardScope.Prefixes)]
    public async Task NativeDataCanBeListedInspectedAndDeletedWithoutChangingBusinessCache(string backend, CacheDashboardScope scope)
    {
        var provider = Create(backend, scope, scope == CacheDashboardScope.Prefixes ? [_root + "ids:", _root + "urn:", _root + "ids:nested:"] : null);
        var admin = provider.GetRequiredService<ISimpleCacheAdmin>();
        await Write(provider, "ids:managed");
        var nativeKeys = new List<string>();
        if (backend == "Redis")
        {
            using var connection = await ConnectionMultiplexer.ConnectAsync(Environment.GetEnvironmentVariable("M_SIMPLE_TEST_REDIS")!);
            var db = connection.GetDatabase();
            foreach (var suffix in new[] { "ids:string", "ids:hash", "urn:list", "urn:set", "urn:zset", "outside:string", "ids:huge" })
                _keys.Add(_root + suffix);
            await db.StringSetAsync(_root + "ids:string", new byte[] { 0, 255, 42 }, TimeSpan.FromMinutes(5));
            await db.HashSetAsync(_root + "ids:hash", [new("field", "value"), new("payload", "ordinary")]);
            await db.ListRightPushAsync(_root + "urn:list", ["one", "two"]);
            await db.SetAddAsync(_root + "urn:set", ["one", "two"]);
            await db.SortedSetAddAsync(_root + "urn:zset", [new("one", 1), new("two", 2)]);
            await db.StringSetAsync(_root + "outside:string", "outside");
            await db.StringSetAsync(_root + "ids:huge", new string('x', 100000));
            nativeKeys.AddRange(new[] { "ids:string", "ids:hash", "urn:list", "urn:set", "urn:zset", "ids:huge" }.Select(s => _root + s));
            var binary = await admin.PeekAsync(_root + "ids:string");
            Assert.IsNotNull(binary!.Entry.ExpiresAt);
            using var json = System.Text.Json.JsonDocument.Parse(binary.Json);
            Assert.AreEqual(Convert.ToBase64String([0, 255, 42]), json.RootElement.GetProperty("values")[0].GetProperty("base64").GetString());
            Assert.AreEqual(System.Text.Json.JsonValueKind.Null, json.RootElement.GetProperty("values")[0].GetProperty("text").ValueKind);
            var large = await admin.PeekAsync(_root + "ids:huge");
            using var largeJson = System.Text.Json.JsonDocument.Parse(large!.Json);
            Assert.IsTrue(largeJson.RootElement.GetProperty("truncated").GetBoolean());
            Assert.IsTrue(large.Json.Length < 170000);
            if (connection.GetEndPoints().Select(endpoint => connection.GetServer(endpoint)).Any(s => s.Version.Major >= 5))
            {
                var streamKey = _root + "urn:stream";
                _keys.Add(streamKey);
                await db.ExecuteAsync("XADD", streamKey, "*", "field", "value");
                nativeKeys.Add(streamKey);
            }
        }
        else
        {
            using var client = new MongoClient(Environment.GetEnvironmentVariable("M_SIMPLE_TEST_MONGODB"));
            var collection = client.GetDatabase(_database).GetCollection<BsonDocument>("CacheEntries");
            await collection.InsertManyAsync(new BsonDocument[]
            {
                new() { ["_id"] = _root + "ids:document", ["name"] = "native", ["expiresAt"] = DateTime.UtcNow.AddDays(-1) },
                new() { ["_id"] = _root + "urn:document", ["list"] = new BsonArray { 1, 2 } },
                new() { ["_id"] = _root + "outside:document", ["flag"] = true },
                new() { ["_id"] = _root + "ids:huge", ["text"] = new string('x', 100000) },
                new() { ["_id"] = ObjectId.GenerateNewId(), ["name"] = "nonstring identifier" },
                new() { ["_id"] = 42, ["name"] = "numeric identifier" }
            });
            nativeKeys.AddRange(new[] { "ids:document", "urn:document", "ids:huge" }.Select(s => _root + s));
            var large = await admin.PeekAsync(_root + "ids:huge");
            using var largeJson = System.Text.Json.JsonDocument.Parse(large!.Json);
            Assert.IsTrue(largeJson.RootElement.GetProperty("previewOmitted").GetBoolean());
            if (scope == CacheDashboardScope.All)
            {
                var all = await Scan(admin);
                var typed = all.Where(i => i.Key.StartsWith("~mongo:", StringComparison.Ordinal)).ToArray();
                Assert.AreEqual(2, typed.Length);
                foreach (var item in typed)
                {
                    Assert.IsNotNull(await admin.PeekAsync(item.Key));
                    Assert.IsTrue(await admin.RemoveAsync(item.Key));
                }
            }
        }
        var entries = await Scan(admin, _root);
        foreach (var key in nativeKeys)
        {
            Assert.IsTrue(entries.Any(i => i.Key == key), "Native entry missing: " + key);
            Assert.IsNotNull(await admin.PeekAsync(key));
        }
        Assert.AreEqual(entries.Count, entries.Select(i => i.Key).Distinct().Count());
        if (scope == CacheDashboardScope.Prefixes)
        {
            Assert.IsTrue(entries.All(i => i.Key.StartsWith(_root + "ids:") || i.Key.StartsWith(_root + "urn:")));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => admin.RemoveAsync(_root + "outside:string"));
        }
        foreach (var key in nativeKeys)
        {
            Assert.IsTrue(await admin.RemoveAsync(key));
            Assert.IsNull(await admin.PeekAsync(key));
        }
        Assert.IsNotNull(await admin.PeekAsync(_root + "ids:managed"));
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        foreach (var provider in _providers) await provider.DisposeAsync();
        if (_backend == "Redis")
        {
            using var connection = await ConnectionMultiplexer.ConnectAsync(Environment.GetEnvironmentVariable("M_SIMPLE_TEST_REDIS")!);
            foreach (var key in _keys) await connection.GetDatabase().KeyDeleteAsync(key);
        }
        if (_backend == "MongoDB")
        {
            using var client = new MongoClient(Environment.GetEnvironmentVariable("M_SIMPLE_TEST_MONGODB"));
            await client.DropDatabaseAsync(_database);
        }
    }
}
