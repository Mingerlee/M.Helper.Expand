using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace M.Simple.AspNetCore.Response;

internal sealed class UnifiedResponseExceptionHandler(
    ILogger<UnifiedResponseExceptionHandler> logger,
    IOptions<UnifiedResponseOptions> options,
    IOptions<JsonOptions> jsonOptions) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var endpoint = context.Features.Get<IExceptionHandlerFeature>()?.Endpoint ?? context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<SkipUnifiedResponseAttribute>() is not null) return false;

        UnhandledExceptionLogging.LogOnce(logger, context, exception);
        var statusCode = exception is SimpleException
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(
            options.Value.Create(statusCode, message: exception is SimpleException ? exception.Message : null),
            jsonOptions.Value.JsonSerializerOptions, cancellationToken);
        return true;
    }
}
