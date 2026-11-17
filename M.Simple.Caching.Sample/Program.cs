using System.Text.Json;
using M.Simple.Caching;
using M.Simple.Caching.MongoDB;
using M.Simple.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;

var backend = args.FirstOrDefault()?.ToLowerInvariant() ?? "memory";
var services = new ServiceCollection();
switch (backend)
{
    case "memory":
        services.AddSimpleMemoryCache(options => options.KeyPrefix = "M.Simple.Caching.Sample:");
        break;
    case "redis":
        services.AddSimpleRedisCache(options =>
        {
            options.ConnectionString = Environment.GetEnvironmentVariable("M_SIMPLE_REDIS") ?? "127.0.0.1:6379";
            options.KeyPrefix = "M.Simple.Caching.Sample:";
        });
        break;
    case "mongo":
        services.AddSimpleMongoCache(options =>
        {
            options.ConnectionString = Environment.GetEnvironmentVariable("M_SIMPLE_MONGODB") ?? "mongodb://127.0.0.1:27017/?directConnection=true";
            options.DatabaseName = Environment.GetEnvironmentVariable("M_SIMPLE_CACHE_DATABASE") ?? "M_Simple_Cache_Sample";
            options.KeyPrefix = "M.Simple.Caching.Sample:";
        });
        break;
    default: throw new ArgumentException("Choose memory, redis or mongo.");
}

await using var provider = services.BuildServiceProvider();
var cache = provider.GetRequiredService<ISimpleCache>();
var calls = 0;
Task<SampleProduct> Load(CancellationToken token)
{
    token.ThrowIfCancellationRequested();
    calls++;
    return Task.FromResult(new SampleProduct(42, "示例商品", 123.45m));
}

var first = await cache.GetOrCreateAsync("product:42", Load);
var second = await cache.GetOrCreateAsync("product:42", Load);
var cached = await cache.TryGetAsync<SampleProduct>("product:42");
await cache.SetAsync<string?>("null-example", null);
var cachedNull = await cache.TryGetAsync<string?>("null-example");
await cache.RemoveAsync("product:42");
await cache.RemoveAsync("null-example");
var removed = !await cache.ExistsAsync("product:42");
if (!cached.Found || !cachedNull.Found || cachedNull.Value is not null || !removed || first != second)
    throw new InvalidOperationException("Cache sample validation failed.");

Console.WriteLine(JsonSerializer.Serialize(new { Backend = backend, First = first, Second = second,
    FactoryCalls = calls, CachedNullFound = cachedNull.Found, Removed = removed }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

public sealed record SampleProduct(int Id, string Name, decimal Amount);
