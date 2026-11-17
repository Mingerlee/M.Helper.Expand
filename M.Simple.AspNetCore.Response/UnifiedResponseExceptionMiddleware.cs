using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace M.Simple.AspNetCore.Response;

// Owns the exception boundary so .NET 8's ExceptionHandlerMiddleware does not emit a second automatic log.
internal sealed class UnifiedResponseExceptionMiddleware(
    RequestDelegate next, IEnumerable<IExceptionHandler> handlers, ILogger<UnifiedResponseExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            if (!context.Response.HasStarted) context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var feature = new ExceptionHandlerFeature
            {
                Error = exception,
                Path = context.Request.Path,
                Endpoint = context.GetEndpoint(),
                RouteValues = context.Request.RouteValues
            };
            context.Features.Set<IExceptionHandlerFeature>(feature);
            context.Features.Set<IExceptionHandlerPathFeature>(feature);
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            foreach (var handler in handlers)
            {
                if (await handler.TryHandleAsync(context, exception, context.RequestAborted)) return;
            }

            // Opted-out endpoints retain an empty 500, with an exception log when not already recorded.
            UnhandledExceptionLogging.LogOnce(logger, context, exception);
        }
    }
}
