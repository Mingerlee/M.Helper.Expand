using M.Simple.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace M.Simple.Caching.Test;

[TestClass]
[TestCategory("RedisCompatibility")]
public sealed class RedisCompatibilityTests
{
    private readonly string _prefix = "m-simple-redis-compat:" + Guid.NewGuid().ToString("N") + ":";
    private readonly HashSet<RedisKey> _keys = new();
    private ServiceProvider? _provider;
    private ConnectionMultiplexer? _connection;
    private IDatabase _database = null!;
    private ISimpleCache _cache = null!;
    private ISimpleCacheAdmin _admin = null!;

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        var address = Environment.GetEnvironmentVariable("M_SIMPLE_TEST_REDIS");
        if (string.IsNullOrWhiteSpace(address)) Assert.Inconclusive("Set M_SIMPLE_TEST_REDIS to a real Redis 3.2+ instance.");
        var configuration = ConfigurationOptions.Parse(address!);
        var selectedDatabase = configuration.DefaultDatabase ?? 0;
        _connection = await ConnectionMultiplexer.ConnectAsync(configuration);
        _database = _connection.GetDatabase(selectedDatabase);
        foreach (var endpoint in _connection.GetEndPoints())
            TestContext.WriteLine("Redis version: " + _connection.GetServer(endpoint).Version + "; database: " + selectedDatabase);

        // Give the provider a different default: the explicit Database option must win.
        configuration.DefaultDatabase = selectedDatabase == 0 ? 1 : 0;
        var services = new ServiceCollection();
        services.AddSimpleRedisCache(options =>
        {
            options.ConnectionString = configuration.ToString();
            options.Database = selectedDatabase;
            options.KeyPrefix = _prefix;
            options.NullExpiration = TimeSpan.FromSeconds(1);
        });
        _provider = services.BuildServiceProvider();
        _cache = _provider.GetRequiredService<ISimpleCache>();
        _admin = _provider.GetRequiredService<ISimpleCacheAdmin>();
    }

    private string Key(string suffix)
    {
        _keys.Add(_prefix + suffix);
        return suffix;
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        try
        {
            if (_connection is not null)
                foreach (var key in _keys) await _database.KeyDeleteAsync(key);
        }
        finally
        {
            if (_provider is not null) await _provider.DisposeAsync();
            _connection?.Dispose();
        }
    }

    [TestMethod]
    public async Task WritesAndUpdatesKeepTheExistingHashFormatAndUseSelectedDatabase()
    {
        var key = Key("product");
        await _cache.SetAsync(key, new Product("旧商品", 1));
        await _cache.SetAsync(key, new Product("新商品", 2));
        Assert.AreEqual(new Product("新商品", 2), (await _cache.TryGetAsync<Product>(key)).Value);
        Assert.IsTrue(await _cache.ExistsAsync(key));
        var fields = await _database.HashGetAsync(_prefix + key, new RedisValue[] { "type", "payload", "absolute", "sliding", "expires" });
        Assert.IsTrue(fields.All(field => field.HasValue), "Existing five-field format must remain compatible.");
        var detail = await _admin.PeekAsync(key);
        Assert.IsNotNull(detail);
        Assert.AreEqual(key, detail.Entry.Key);
        Assert.AreEqual(((byte[])fields[1]!).LongLength, detail.Entry.SizeBytes);
    }

    [TestMethod]
    public async Task SlidingRefreshWorksAndManagementReadsDoNotRenewExpiration()
    {
        var key = Key("sliding");
        await _cache.SetAsync(key, 42, new() { SlidingExpiration = TimeSpan.FromSeconds(2), AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5) });
        var first = await _admin.PeekAsync(key);
        await Task.Delay(400);
        Assert.IsTrue((await _cache.TryGetAsync<int>(key)).Found);
        var refreshed = await _admin.PeekAsync(key);
        Assert.IsTrue(refreshed!.Entry.ExpiresAt > first!.Entry.ExpiresAt);
        var peek = await _admin.PeekAsync(key);
        Assert.AreEqual(refreshed.Entry.ExpiresAt, peek!.Entry.ExpiresAt);
        await Task.Delay(2300);
        Assert.IsFalse(await _cache.ExistsAsync(key));
        Assert.IsNull(await _admin.PeekAsync(key));
    }

    [TestMethod]
    public async Task FactoryLoadingAndAbsoluteExpirationWork()
    {
        var key = Key("factory");
        var calls = 0;
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => _cache.GetOrCreateAsync(key, async ct =>
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(100, ct);
            return Guid.NewGuid();
        }, new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1) })));
        Assert.AreEqual(1, calls);
        Assert.AreEqual(1, results.Distinct().Count());
        await Task.Delay(1300);
        Assert.IsFalse((await _cache.TryGetAsync<Guid>(key)).Found);
    }

    [TestMethod]
    public async Task AdminPaginationAndRemovalWorkWithoutDeletingForeignData()
    {
        var expected = Enumerable.Range(0, 6).Select(i => Key("entries:" + i)).ToHashSet(StringComparer.Ordinal);
        foreach (var key in expected) await _cache.SetAsync(key, key);
        var foreignString = Key("entries:foreign-string");
        var foreignHash = Key("entries:foreign-hash");
        await _database.StringSetAsync(_prefix + foreignString, "foreign");
        await _database.HashSetAsync(_prefix + foreignHash, "field", "value");
        var actual = new HashSet<string>(StringComparer.Ordinal);
        string? cursor = null;
        var pages = 0;
        do
        {
            var page = await _admin.ScanAsync("entries:", cursor, 2);
            foreach (var entry in page.Items) actual.Add(entry.Key);
            cursor = page.NextCursor;
            Assert.IsTrue(++pages < 1000);
        } while (cursor is not null);
        Assert.IsTrue(expected.SetEquals(actual));
        foreach (var key in expected)
        {
            Assert.IsTrue(await _admin.RemoveAsync(key));
            Assert.IsFalse(await _admin.RemoveAsync(key));
            Assert.IsFalse(await _cache.ExistsAsync(key));
        }
        Assert.IsFalse(await _admin.RemoveAsync(foreignString));
        Assert.IsFalse(await _admin.RemoveAsync(foreignHash));
        Assert.IsTrue(await _database.KeyExistsAsync(_prefix + foreignString));
        Assert.IsTrue(await _database.KeyExistsAsync(_prefix + foreignHash));
    }

    [TestMethod]
    public async Task NeverExpiresAndBoundedNullEntriesWork()
    {
        var forever = Key("forever");
        var empty = Key("null");
        await _cache.SetAsync(forever, 1, new() { NeverExpires = true });
        Assert.IsNull(await _database.KeyTimeToLiveAsync(_prefix + forever));
        await _cache.SetAsync<string?>(empty, null, new() { NeverExpires = true });
        var result = await _cache.TryGetAsync<string?>(empty);
        Assert.IsTrue(result.Found);
        Assert.IsNull(result.Value);
        await Task.Delay(1300);
        Assert.IsFalse(await _cache.ExistsAsync(empty));
        Assert.IsTrue(await _cache.ExistsAsync(forever));
        await _cache.RemoveAsync(forever);
        Assert.IsFalse(await _cache.ExistsAsync(forever));
    }

    public sealed record Product(string Name, int Count);
}
