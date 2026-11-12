namespace M.Helper.Logging;

/// <summary>日志快照。时间统一使用 UTC，属性和作用域使用 JSON 保存。</summary>
public sealed class LogEntry
{
    public long Id { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public int Level { get; set; }
    public string Category { get; set; } = "";
    public int EventId { get; set; }
    public string? EventName { get; set; }
    public string MessageTemplate { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Exception { get; set; }
    public string PropertiesJson { get; set; } = "{}";
    public string ScopesJson { get; set; } = "[]";
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string ApplicationName { get; set; } = "";
    public string InstanceId { get; set; } = "";
}

/// <summary>按 Id 倒序的游标分页条件。时间范围为包含起点、不包含终点。</summary>
public sealed class LogQuery
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int? Level { get; set; }
    public int? MinimumLevel { get; set; }
    public string? Category { get; set; }
    public string? Keyword { get; set; }
    public string? TraceId { get; set; }
    public long? BeforeId { get; set; }
    public int PageSize { get; set; } = 50;
}

public sealed record LogPage(IReadOnlyList<LogEntry> Items, long? NextBeforeId);
public sealed record LogTrendPoint(DateTimeOffset TimestampUtc, long Total, long Errors);
public sealed record LogStatistics(long Total, long Today, long TodayErrors, long LastHour,
    IReadOnlyList<LogTrendPoint> Trend);
public sealed record LoggingStatus(long Accepted, long Persisted, long Dropped, long WriteFailures,
    long CaptureFailures, string? LastError, DateTimeOffset? LastWriteUtc, bool IsWriting)
{
    public LogStorageStatus? Storage { get; init; }
}

/// <summary>最近一次容量检查快照。可用空间无法读取时为 null，暂停原因使用稳定代码。</summary>
public sealed record LogStorageStatus(long DatabaseBytes, long WalBytes, long SharedMemoryBytes,
    long MaxStorageBytes, long? AvailableDiskBytes, long MinFreeDiskSpaceBytes,
    bool IsPaused, string? PauseReason, long PrunedLogs, DateTimeOffset CheckedAtUtc)
{
    public long TotalBytes => DatabaseBytes + WalBytes + SharedMemoryBytes;
}
