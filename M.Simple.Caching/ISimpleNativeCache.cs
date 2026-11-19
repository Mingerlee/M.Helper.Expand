using System.Text;
using System.Text.Json;

namespace M.Simple.Caching;

/// <summary>Read-only native data access. Keys are complete storage keys and reads never renew expiry.</summary>
public interface ISimpleNativeCache
{
    Task<CacheResult<T>> TryGetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<CacheResult<IReadOnlyDictionary<string, byte[]>>> HashGetAllAsync(string key, CancellationToken cancellationToken = default);
    Task<CacheResult<IReadOnlyList<byte[]>>> ListRangeAsync(string key, long start = 0, int count = 100, CancellationToken cancellationToken = default);
    Task<CacheResult<IReadOnlyList<byte[]>>> SetMembersAsync(string key, CancellationToken cancellationToken = default);
    Task<CacheResult<IReadOnlyList<NativeSortedSetEntry>>> SortedSetRangeAsync(string key, long start = 0, int count = 100, CancellationToken cancellationToken = default);
}

public sealed record NativeSortedSetEntry(byte[] Member, double Score);

/// <summary>Optional backend native read capability. Implementations enforce payload limits before returning data.</summary>
public interface ISimpleNativeCacheStore
{
    Task<CacheResult<T>> ReadNativeAsync<T>(string key, SimpleNativeCacheOptions options, CancellationToken cancellationToken);
    Task<bool> NativeExistsAsync(string key, CancellationToken cancellationToken);
}

/// <summary>Optional Redis-style collection capability. Other backends explicitly reject these operations.</summary>
public interface ISimpleNativeCollectionStore
{
    Task<CacheResult<IReadOnlyList<byte[]>>> ReadCollectionAsync(string key, NativeCollectionKind kind, long start, int count,
        SimpleNativeCacheOptions options, CancellationToken cancellationToken);
}

public enum NativeCollectionKind { Hash, List, Set, SortedSet }

/// <summary>Shared byte/string/JSON conversion for native byte stores.</summary>
public static class NativeCacheSerialization
{
    public static T? Deserialize<T>(byte[] bytes, JsonSerializerOptions options, bool rawText = false)
    {
        if (typeof(T) == typeof(byte[])) return (T)(object)bytes.ToArray();
        if (rawText && typeof(T) == typeof(string)) return (T)(object)new UTF8Encoding(false, true).GetString(bytes);
        return JsonSerializer.Deserialize<T>(bytes, options);
    }
}
