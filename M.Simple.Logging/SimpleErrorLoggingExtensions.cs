using System.Collections;
using Microsoft.Extensions.Logging;

namespace M.Simple.Logging;

/// <summary>对当前这一条 Error 日志选择是否附带已采集的请求信息，不修改全局配置。</summary>
public static class SimpleErrorLoggingExtensions
{
    /// <summary>记录错误；includeRequestContext 仅控制本条日志。</summary>
    public static void LogError(this ILogger logger, bool includeRequestContext, string? message, params object?[] args) =>
        LoggerExtensions.LogError(Controlled(logger, includeRequestContext), message, args);

    /// <summary>记录带异常的错误；includeRequestContext 仅控制本条日志。</summary>
    public static void LogError(this ILogger logger, bool includeRequestContext, Exception? exception,
        string? message, params object?[] args) =>
        LoggerExtensions.LogError(Controlled(logger, includeRequestContext), exception, message, args);

    /// <summary>记录带事件号的错误；includeRequestContext 仅控制本条日志。</summary>
    public static void LogError(this ILogger logger, bool includeRequestContext, EventId eventId,
        string? message, params object?[] args) =>
        LoggerExtensions.LogError(Controlled(logger, includeRequestContext), eventId, message, args);

    /// <summary>记录带事件号和异常的错误；includeRequestContext 仅控制本条日志。</summary>
    public static void LogError(this ILogger logger, bool includeRequestContext, EventId eventId,
        Exception? exception, string? message, params object?[] args) =>
        LoggerExtensions.LogError(Controlled(logger, includeRequestContext), eventId, exception, message, args);

    private static ILogger Controlled(ILogger logger, bool include)
    {
        ArgumentNullException.ThrowIfNull(logger);
        return new ControlledLogger(logger, include);
    }

    private sealed class ControlledLogger(ILogger logger, bool include) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => logger.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => logger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // 控制标记仅存在于这一条日志的 State，不用全局开关或 AsyncLocal 临时覆盖，避免串日志。
            var wrapped = new ControlledState<TState>(state, include);
            logger.Log(logLevel, eventId, wrapped, exception, (value, ex) => formatter(value.Original, ex));
        }
    }

    private sealed class ControlledState<TState>(TState original, bool include)
        : IReadOnlyList<KeyValuePair<string, object?>>, IRequestContextLogState
    {
        private readonly IReadOnlyList<KeyValuePair<string, object?>> _properties =
            original as IReadOnlyList<KeyValuePair<string, object?>> ??
            (original as IEnumerable<KeyValuePair<string, object?>>)?.ToArray() ?? [];

        internal TState Original => original;
        public bool IncludeRequestContext => include;
        public int Count => _properties.Count;
        public KeyValuePair<string, object?> this[int index] => _properties[index];
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _properties.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public override string? ToString() => original?.ToString();
    }
}

internal interface IRequestContextLogState
{
    bool IncludeRequestContext { get; }
}
