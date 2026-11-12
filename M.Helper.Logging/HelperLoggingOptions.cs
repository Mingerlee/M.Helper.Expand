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
            !Enum.IsDefined(MinimumLevel))
        {
            throw new ArgumentException("日志容量、批量大小、保留时间、字段长度或日志级别配置无效。");
        }
    }
}
