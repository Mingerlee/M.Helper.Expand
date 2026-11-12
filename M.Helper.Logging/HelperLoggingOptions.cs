using Microsoft.Extensions.Logging;

namespace M.Helper.Logging;

/// <summary>日志存储及后台写入选项；启动后不动态变更。</summary>
public sealed class HelperLoggingOptions
{
    public string ApplicationName { get; set; } = AppDomain.CurrentDomain.FriendlyName;
    public string DatabasePath { get; set; } = "App_Data/helper-logs.db";
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
    public int RetentionDays { get; set; } = 15;
    public int QueueCapacity { get; set; } = 10_000;
    public int BatchSize { get; set; } = 200;
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxFieldLength { get; set; } = 32_768;

    /// <summary>数据库、WAL、SHM 文件合计的保护阈值，默认 512 MiB，至少 16 MiB。</summary>
    public long MaxStorageBytes { get; set; } = 512L * 1024 * 1024;
    /// <summary>所在文件系统最低可用空间，默认 256 MiB。</summary>
    public long MinFreeDiskSpaceBytes { get; set; } = 256L * 1024 * 1024;
    /// <summary>低磁盘暂停后，额外恢复余量，避免临界值附近频繁启停。</summary>
    public long DiskSpaceRecoveryMarginBytes { get; set; } = 64L * 1024 * 1024;
    /// <summary>容量维护及暂停后重新检查的间隔，默认 30 秒。</summary>
    public TimeSpan StorageCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>在入队前处理消息、异常和属性，例如业务脱敏；回调应快速且线程安全。</summary>
    public Action<LogEntry>? Enrich { get; set; }

    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ApplicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(DatabasePath);
        if (RetentionDays < 1 || RetentionDays > 3650 || QueueCapacity < 1 ||
            BatchSize < 1 || BatchSize > QueueCapacity || MaxFieldLength < 256 ||
            FlushInterval <= TimeSpan.Zero || FlushInterval > TimeSpan.FromMinutes(1) ||
            ShutdownTimeout <= TimeSpan.Zero || ShutdownTimeout > TimeSpan.FromMinutes(1) ||
            MaxStorageBytes < 16L * 1024 * 1024 || MaxStorageBytes > 1024L * 1024 * 1024 * 1024 ||
            MinFreeDiskSpaceBytes < 0 || DiskSpaceRecoveryMarginBytes < 0 ||
            MinFreeDiskSpaceBytes > long.MaxValue - DiskSpaceRecoveryMarginBytes ||
            StorageCheckInterval <= TimeSpan.Zero || StorageCheckInterval > TimeSpan.FromHours(1) ||
            !Enum.IsDefined(MinimumLevel))
        {
            throw new ArgumentException("日志容量、批量大小、保留时间、字段长度或日志级别配置无效。");
        }
    }
}
