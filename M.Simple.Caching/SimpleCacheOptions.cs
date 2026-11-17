using System.Text.Json;

namespace M.Simple.Caching;

public class SimpleCacheOptions
{
    public string KeyPrefix { get; set; } = "";
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(10);
    public bool CacheNullValues { get; set; }
    public TimeSpan NullExpiration { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan FactoryTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxPayloadBytes { get; set; } = 1024 * 1024;
    public JsonSerializerOptions SerializerOptions { get; set; } = new(JsonSerializerDefaults.Web);

    public virtual void Validate()
    {
        ArgumentNullException.ThrowIfNull(KeyPrefix);
        ArgumentNullException.ThrowIfNull(SerializerOptions);
        if (KeyPrefix.Length > 512) throw new ArgumentException("KeyPrefix must be at most 512 characters.");
        ValidateExpiration(DefaultExpiration, nameof(DefaultExpiration));
        ValidateExpiration(NullExpiration, nameof(NullExpiration));
        if (FactoryTimeout < TimeSpan.FromMilliseconds(1) || FactoryTimeout > TimeSpan.FromDays(1))
            throw new ArgumentOutOfRangeException(nameof(FactoryTimeout));
        if (MaxPayloadBytes < 1) throw new ArgumentOutOfRangeException(nameof(MaxPayloadBytes));
    }

    public static void ValidateExpiration(TimeSpan duration, string name)
    {
        if (duration < TimeSpan.FromMilliseconds(1) || duration > TimeSpan.FromDays(3650))
            throw new ArgumentOutOfRangeException(name, "Expiration must be between 1 millisecond and 3650 days.");
    }

    internal SimpleCacheOptions Snapshot() => new()
    {
        KeyPrefix = KeyPrefix,
        DefaultExpiration = DefaultExpiration,
        CacheNullValues = CacheNullValues,
        NullExpiration = NullExpiration,
        FactoryTimeout = FactoryTimeout,
        MaxPayloadBytes = MaxPayloadBytes,
        SerializerOptions = new(SerializerOptions)
    };
}

public sealed class SimpleMemoryCacheOptions : SimpleCacheOptions
{
    public int MaxEntryCount { get; set; } = 10_000;
    public long MaxSizeBytes { get; set; } = 64L * 1024 * 1024;

    public override void Validate()
    {
        base.Validate();
        if (MaxEntryCount < 1) throw new ArgumentOutOfRangeException(nameof(MaxEntryCount));
        if (MaxSizeBytes < 1) throw new ArgumentOutOfRangeException(nameof(MaxSizeBytes));
    }
}

public sealed class SimpleCacheEntryOptions
{
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public bool NeverExpires { get; set; }
    /// <summary>Overrides automatic null-result caching for GetOrCreateAsync.</summary>
    public bool? CacheNullValues { get; set; }

    internal SimpleCacheEntryOptions Snapshot()
    {
        if (NeverExpires && (AbsoluteExpirationRelativeToNow.HasValue || SlidingExpiration.HasValue))
            throw new ArgumentException("NeverExpires cannot be combined with an expiration.");
        if (AbsoluteExpirationRelativeToNow is { } absolute)
            SimpleCacheOptions.ValidateExpiration(absolute, nameof(AbsoluteExpirationRelativeToNow));
        if (SlidingExpiration is { } sliding)
            SimpleCacheOptions.ValidateExpiration(sliding, nameof(SlidingExpiration));
        return new()
        {
            AbsoluteExpirationRelativeToNow = AbsoluteExpirationRelativeToNow,
            SlidingExpiration = SlidingExpiration,
            NeverExpires = NeverExpires,
            CacheNullValues = CacheNullValues
        };
    }
}
