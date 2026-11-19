using M.Simple.Caching;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace M.Simple.Caching.Redis;

public sealed class SimpleRedisCacheOptions : SimpleCacheOptions
{
    public string ConnectionString { get; set; } = "";
    public int Database { get; set; } = -1;

    public override void Validate()
    {
        base.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(ConnectionString);
        _ = ConfigurationOptions.Parse(ConnectionString);
        if (Database < -1) throw new ArgumentOutOfRangeException(nameof(Database));
    }
}

public static class SimpleRedisCacheServiceCollectionExtensions
{
    public static IServiceCollection AddSimpleRedisCache(this IServiceCollection services, Action<SimpleRedisCacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new SimpleRedisCacheOptions();
        configure(options);
        return services.AddSimpleCacheStore(options, sp => new RedisCacheStore(options,
            sp.GetRequiredService<SimpleCacheManagementOptions>().Scope));
    }
}
