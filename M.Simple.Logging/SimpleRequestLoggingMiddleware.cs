using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace M.Simple.Logging;

/// <summary>为错误日志提供请求参数快照，记录未处理异常并保留原有异常传播。</summary>
internal sealed class SimpleRequestLoggingMiddleware(RequestDelegate next, SimpleRequestLoggingOptions options,
    ILogger<SimpleRequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext http)
    {
        var capture = true;
        try { capture = options.ShouldCapture?.Invoke(http) ?? true; }
        catch (Exception) { capture = false; }
        if (!capture)
        {
            await next(http);
            return;
        }
        var context = new RequestLogContext(http, options);
        using var lease = RequestLogContext.Push(context);
        try { await context.CaptureBodyAsync(); }
        catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception) { /* 请求采集失败不得阻止下游处理。 */ }
        try { await next(http); }
        catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            if (options.LogUnhandledExceptions)
                logger.LogError(new EventId(1001, "UnhandledRequestException"), ex, "接口发生未处理异常");
            throw;
        }
    }
}
