namespace M.Simple.Caching;

/// <summary>The cache entries visible to management operations, independent of business key prefixing.</summary>
public enum CacheDashboardScope
{
    CurrentApplication,
    Prefixes,
    /// <summary>All entries in the configured storage, including data not written by this package.</summary>
    All,
    /// <summary>All applications' entries that match this package's storage format.</summary>
    AllManaged
}

/// <summary>Shared management scope for memory, Redis and MongoDB.</summary>
public class SimpleCacheManagementOptions
{
    public CacheDashboardScope Scope { get; set; } = CacheDashboardScope.CurrentApplication;
    public string[] Prefixes { get; set; } = [];

    internal SimpleCacheManagementOptions Snapshot()
    {
        if (!Enum.IsDefined(Scope)) throw new ArgumentException("Unknown cache management scope.");
        if (Prefixes is null) throw new ArgumentException("Prefixes cannot be null.");
        if (Scope == CacheDashboardScope.Prefixes)
        {
            if (Prefixes.Length is < 1 or > 64 || Prefixes.Any(p => string.IsNullOrWhiteSpace(p) || p.Length > 512))
                throw new ArgumentException("Specify 1-64 nonempty prefixes, each at most 512 characters.");
        }
        else if (Prefixes.Length != 0)
            throw new ArgumentException("Prefixes may only be configured in Prefixes scope.");

        // Removing covered prefixes makes each partition disjoint and avoids duplicate entries.
        var normalized = new List<string>();
        foreach (var prefix in Prefixes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            if (!normalized.Any(p => prefix.StartsWith(p, StringComparison.Ordinal))) normalized.Add(prefix);
        return new() { Scope = Scope, Prefixes = normalized.ToArray() };
    }
}
