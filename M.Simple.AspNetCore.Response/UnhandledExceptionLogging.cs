using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace M.Simple.AspNetCore.Response;

internal static class UnhandledExceptionLogging
{
    // Shared HTTP Items contract with M.Simple.Logging; no package dependency is required.
    internal const string LoggedExceptionItemKey = "M.Simple.UnhandledException.LoggedException";

    internal static void LogOnce(ILogger logger, HttpContext context, Exception exception)
    {
        if (ReferenceEquals(context.Items[LoggedExceptionItemKey], exception) || !logger.IsEnabled(LogLevel.Error)) return;
        logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        context.Items[LoggedExceptionItemKey] = exception;
    }
}
