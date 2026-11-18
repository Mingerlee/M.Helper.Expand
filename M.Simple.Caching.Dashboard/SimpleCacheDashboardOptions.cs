namespace M.Simple.Caching.Dashboard;

public sealed class SimpleCacheDashboardOptions : SimpleCacheManagementOptions
{
    /// <summary>Disables all deletion and preview endpoints.</summary>
    public bool ReadOnly { get; set; }
}
