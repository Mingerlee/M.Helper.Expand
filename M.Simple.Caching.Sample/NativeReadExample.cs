using System.Text;
using System.Text.Json;
using M.Simple.Caching;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using StackExchange.Redis;

internal static class NativeReadExample
{
    public static async Task RunAsync(IServiceCollection services, string backend)
    {
        // This configuration is independent from AddSimpleCacheDashboard, and can be registered before or after the backend.
        services.AddSimpleNativeCache(options =>
        {
            options.Scope = NativeCacheScope.CurrentApplication;
            options.MaxPayloadBytes = 1024 * 1024;
            options.MaxCollectionItems = 1000;
        });
        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ISimpleCache>();
        var native = provider.GetRequiredService<ISimpleNativeCache>();
        var relativeKey = "native:" + Guid.NewGuid().ToString("N") + ":product";
        var fullKey = "M.Simple.Caching.Sample:" + relativeKey;
        var product = new NativeProduct { Name = "原生读取示例", Price = 88.5m };
        ConnectionMultiplexer? redis = null;
        MongoClient? mongo = null;
        IMongoCollection<BsonDocument>? collection = null;
        try
        {
            if (backend == "redis")
            {
                redis = await ConnectionMultiplexer.ConnectAsync(Environment.GetEnvironmentVariable("M_SIMPLE_REDIS") ?? "127.0.0.1:6379");
                var db = redis.GetDatabase();
                // Simulate data written by another application; use a Redis String instead of this package's Hash format.
                await db.StringSetAsync(fullKey, JsonSerializer.Serialize(product, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
                await db.KeyExpireAsync(fullKey, TimeSpan.FromMinutes(2));
                await db.HashSetAsync(fullKey + ":hash", "name", "原生 Hash");
                await db.KeyExpireAsync(fullKey + ":hash", TimeSpan.FromMinutes(2));
                await db.ListRightPushAsync(fullKey + ":list", ["first", "second"]);
                await db.KeyExpireAsync(fullKey + ":list", TimeSpan.FromMinutes(2));
                var hash = await native.HashGetAllAsync(fullKey + ":hash");
                var list = await native.ListRangeAsync(fullKey + ":list", 0, 2);
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    HashName = Encoding.UTF8.GetString(hash.Value!["name"]),
                    List = list.Value!.Select(value => Encoding.UTF8.GetString(value))
                }));
            }
            else if (backend == "mongo")
            {
                mongo = new MongoClient(Environment.GetEnvironmentVariable("M_SIMPLE_MONGODB") ?? "mongodb://127.0.0.1:27017/?directConnection=true");
                collection = mongo.GetDatabase(Environment.GetEnvironmentVariable("M_SIMPLE_CACHE_DATABASE") ?? "M_Simple_Cache_Sample")
                    .GetCollection<BsonDocument>("CacheEntries");
                // Native reads use the entire document and the BSON serializer, not a package payload wrapper.
                await collection.InsertOneAsync(new() { ["_id"] = fullKey, ["name"] = product.Name, ["price"] = new Decimal128(product.Price) });
            }
            else await cache.SetAsync(relativeKey, product);

            var result = await native.TryGetAsync<NativeProduct>(fullKey);
            if (!result.Found || result.Value?.Name != product.Name || result.Value.Price != product.Price)
                throw new InvalidOperationException("Native read sample failed.");
            Console.WriteLine(JsonSerializer.Serialize(new { Backend = backend, FullKey = fullKey, result.Found, result.Value }));
        }
        finally
        {
            if (redis is not null)
            {
                await redis.GetDatabase().KeyDeleteAsync(new RedisKey[] { fullKey, fullKey + ":hash", fullKey + ":list" });
                redis.Dispose();
            }
            if (collection is not null)
                await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", fullKey));
            mongo?.Dispose();
            if (backend == "memory") await cache.RemoveAsync(relativeKey);
        }
    }

    [BsonIgnoreExtraElements]
    public sealed class NativeProduct
    {
        [BsonElement("name")]
        public string Name { get; set; } = "";
        [BsonElement("price")]
        public decimal Price { get; set; }
    }
}
