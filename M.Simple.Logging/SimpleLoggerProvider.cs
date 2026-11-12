using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace M.Simple.Logging;

/// <summary>标准 ILogger 的 SQLite 采集端；不向自身记录内部错误。</summary>
[ProviderAlias("Simple")]
public sealed class SimpleLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly SimpleLoggingOptions _options;
    private readonly LogBuffer _buffer;
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();
    private readonly string _instanceId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    private int _disposed;

    internal SimpleLoggerProvider(SimpleLoggingOptions options, LogBuffer buffer)
    {
        _options = options;
        _buffer = buffer;
    }

    public ILogger CreateLogger(string categoryName) => new SimpleLogger(this, categoryName);
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;
    public void Dispose() => Interlocked.Exchange(ref _disposed, 1);

    private sealed class SimpleLogger(SimpleLoggerProvider owner, string category) : ILogger
    {
        public bool IsEnabled(LogLevel level) => Volatile.Read(ref owner._disposed) == 0 &&
            level != LogLevel.None && level >= owner._options.MinimumLevel;

#if NET6_0
        public IDisposable BeginScope<TState>(TState state) => owner._scopes.Push(state);
#else
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => owner._scopes.Push(state);
#endif

        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(level)) return;
            if (Volatile.Read(ref owner._buffer.Storage)?.IsPaused == true)
            {
                Interlocked.Increment(ref owner._buffer.Dropped);
                return;
            }
            try
            {
                var properties = Snapshot(state);
                var scopes = new List<object?>();
                owner._scopes.ForEachScope((scope, list) => list.Add(Snapshot(scope)), scopes);
                var entry = new LogEntry
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Level = (int)level,
                    Category = category,
                    EventId = eventId.Id,
                    EventName = eventId.Name,
                    Message = formatter(state, exception),
                    MessageTemplate = state is IEnumerable<KeyValuePair<string, object?>> pairs
                        ? pairs.FirstOrDefault(p => p.Key == "{OriginalFormat}").Value?.ToString() ?? "" : "",
                    Exception = exception?.ToString(),
                    PropertiesJson = JsonSerializer.Serialize(properties),
                    ScopesJson = JsonSerializer.Serialize(scopes),
                    TraceId = Activity.Current?.TraceId.ToString(),
                    SpanId = Activity.Current?.SpanId.ToString(),
                    ApplicationName = owner._options.ApplicationName,
                    InstanceId = owner._instanceId
                };
                owner._options.Enrich?.Invoke(entry);
                // 在业务线程生成不可变的字符串快照，避免后台访问已经释放的请求对象。
                entry.Message = Limit(entry.Message);
                entry.MessageTemplate = Limit(entry.MessageTemplate);
                entry.Exception = entry.Exception is null ? null : Limit(entry.Exception);
                entry.Category = Limit(entry.Category);
                entry.EventName = entry.EventName is null ? null : Limit(entry.EventName);
                entry.ApplicationName = Limit(entry.ApplicationName);
                entry.InstanceId = Limit(entry.InstanceId);
                entry.TraceId = entry.TraceId is null ? null : Limit(entry.TraceId);
                entry.SpanId = entry.SpanId is null ? null : Limit(entry.SpanId);
                // JSON 不直接截断，超限时保留有效 JSON 结构和明确的截断标识。
                if (entry.PropertiesJson.Length > owner._options.MaxFieldLength)
                    entry.PropertiesJson = "{\"_truncated\":true}";
                if (entry.ScopesJson.Length > owner._options.MaxFieldLength)
                    entry.ScopesJson = "[\"[truncated]\"]";
                owner._buffer.Enqueue(entry);
            }
            catch (Exception)
            {
                // formatter、业务属性和脱敏回调失败均不得打断业务请求。
                Interlocked.Increment(ref owner._buffer.CaptureFailures);
            }
        }

        private string Limit(string text) => text.Length <= owner._options.MaxFieldLength
            ? text : text[..owner._options.MaxFieldLength] + " [truncated]";

        private static object? Snapshot(object? value)
        {
            if (value is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                var result = new Dictionary<string, object?>();
                foreach (var pair in pairs)
                {
                    if (pair.Key != "{OriginalFormat}") result[pair.Key] = Scalar(pair.Value);
                }
                return result;
            }
            return Scalar(value);
        }

        private static object? Scalar(object? value) => value switch
        {
            null => null,
            string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or
                decimal or Guid or DateTime or DateTimeOffset => value,
            float f when float.IsFinite(f) => f,
            double d when double.IsFinite(d) => d,
            _ => value.ToString()
        };
    }
}

internal sealed class LogBuffer
{
    internal readonly Channel<LogEntry> Channel;
    internal long Accepted, Persisted, Dropped, WriteFailures, CaptureFailures;
    internal string? LastError;
    internal long LastWriteTicks;
    internal int IsWriting;
    internal LogStorageStatus? Storage;

    internal LogBuffer(SimpleLoggingOptions options)
    {
        Channel = System.Threading.Channels.Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(options.QueueCapacity)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        });
    }

    internal void Enqueue(LogEntry entry)
    {
        if (Volatile.Read(ref Storage)?.IsPaused != true && Channel.Writer.TryWrite(entry)) Interlocked.Increment(ref Accepted);
        else Interlocked.Increment(ref Dropped);
    }

    internal LoggingStatus Status() => new(Interlocked.Read(ref Accepted), Interlocked.Read(ref Persisted),
        Interlocked.Read(ref Dropped), Interlocked.Read(ref WriteFailures), Interlocked.Read(ref CaptureFailures),
        Volatile.Read(ref LastError), Interlocked.Read(ref LastWriteTicks) is var ticks && ticks > 0
            ? new DateTimeOffset(ticks, TimeSpan.Zero) : null, Volatile.Read(ref IsWriting) == 1)
        { Storage = Volatile.Read(ref Storage) };
}
