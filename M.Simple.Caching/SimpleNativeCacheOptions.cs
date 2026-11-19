using System.Text.Json;

namespace M.Simple.Caching;

public enum NativeCacheScope { CurrentApplication, Prefixes, All }

/// <summary>Business native read permissions, independent of dashboard management scope.</summary>
public sealed class SimpleNativeCacheOptions
{
    public NativeCacheScope Scope { get; set; } = NativeCacheScope.CurrentApplication;
    public string[] Prefixes { get; set; } = [];
    public int MaxPayloadBytes { get; set; } = 1024 * 1024;
    public int MaxCollectionItems { get; set; } = 1000;
    public JsonSerializerOptions SerializerOptions { get; set; } = new(JsonSerializerDefaults.Web);

    internal SimpleNativeCacheOptions Snapshot()
    {
        if (!Enum.IsDefined(Scope) || Prefixes is null || SerializerOptions is null)
            throw new ArgumentException("Invalid native cache configuration.");
        if (MaxPayloadBytes is < 1 or > 64 * 1024 * 1024 || MaxCollectionItems is < 1 or > 10000)
            throw new ArgumentException("Native read limits must be 1 byte-64 MiB and 1-10000 items respectively.");
        if (Scope == NativeCacheScope.Prefixes)
        {
            if (Prefixes.Length is < 1 or > 64 || Prefixes.Any(p => string.IsNullOrWhiteSpace(p) || p.Length > 512))
                throw new ArgumentException("Specify 1-64 nonempty native prefixes, each at most 512 characters.");
        }
        else if (Prefixes.Length != 0) throw new ArgumentException("Native prefixes require Prefixes scope.");
        return new()
        {
            Scope = Scope, Prefixes = Prefixes.Distinct(StringComparer.Ordinal).ToArray(),
            MaxPayloadBytes = MaxPayloadBytes, MaxCollectionItems = MaxCollectionItems,
            SerializerOptions = new(SerializerOptions)
        };
    }
}
