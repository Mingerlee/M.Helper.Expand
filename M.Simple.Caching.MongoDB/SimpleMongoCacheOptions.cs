using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace M.Simple.Caching.MongoDB;

public sealed class SimpleMongoCacheOptions : SimpleCacheOptions
{
    public string ConnectionString { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public string CollectionName { get; set; } = "CacheEntries";
    public TimeSpan ServerSelectionTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public override void Validate()
    {
        base.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(ConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(DatabaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(CollectionName);
        _ = MongoClientSettings.FromConnectionString(ConnectionString);
        if (ServerSelectionTimeout < TimeSpan.FromMilliseconds(1) || ServerSelectionTimeout > TimeSpan.FromMinutes(5))
            throw new ArgumentOutOfRangeException(nameof(ServerSelectionTimeout));
    }
}

public static class SimpleMongoCacheServiceCollectionExtensions
{
    public static IServiceCollection AddSimpleMongoCache(this IServiceCollection services, Action<SimpleMongoCacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new SimpleMongoCacheOptions();
        configure(options);
        return services.AddSimpleCacheStore(options, sp => new MongoCacheStore(options,
            sp.GetRequiredService<SimpleCacheManagementOptions>().Scope));
    }
}
