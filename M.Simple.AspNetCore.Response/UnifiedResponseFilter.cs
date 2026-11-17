using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace M.Simple.AspNetCore.Response;

internal sealed class UnifiedResponseFilter(IOptions<UnifiedResponseOptions> options) : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.HttpContext.Response.HasStarted || context.ActionDescriptor.EndpointMetadata.OfType<SkipUnifiedResponseAttribute>().Any()) return;

        switch (context.Result)
        {
            case ObjectResult result when result.Value is not IApiResponse:
                var status = result.StatusCode
                    ?? (result.Value as ProblemDetails)?.Status
                    ?? context.HttpContext.Response.StatusCode;
                if (!CanHaveBody(status)) return;
                status = NormalizeStatus(status);
                var data = result.Value is ProblemDetails
                    ? (result.Value as ValidationProblemDetails)?.Errors
                    : result.Value;
                result.Value = options.Value.Create(status, data);
                result.DeclaredType = result.Value.GetType();
                result.StatusCode = status;
                result.ContentTypes.Clear();
                result.ContentTypes.Add("application/json");
                break;
            case JsonResult result when result.Value is not IApiResponse:
                var jsonStatus = result.StatusCode ?? context.HttpContext.Response.StatusCode;
                if (!CanHaveBody(jsonStatus)) return;
                result.StatusCode = NormalizeStatus(jsonStatus);
                result.Value = options.Value.Create(result.StatusCode.Value, result.Value);
                break;
            case StatusCodeResult result when CanHaveBody(result.StatusCode):
                var code = NormalizeStatus(result.StatusCode);
                context.Result = new ObjectResult(options.Value.Create(code)) { StatusCode = code };
                break;
            case EmptyResult:
                var emptyStatus = context.HttpContext.Response.StatusCode;
                if (!CanHaveBody(emptyStatus)) return;
                emptyStatus = NormalizeStatus(emptyStatus);
                context.Result = new ObjectResult(options.Value.Create(emptyStatus)) { StatusCode = emptyStatus };
                break;
        }
    }

    // 204 is normalized to 200 to allow the agreed JSON envelope. Redirects and 304 keep HTTP semantics.
    private static int NormalizeStatus(int status) => status == 204 ? 200 : status;
    private static bool CanHaveBody(int status) => status >= 200 && (status < 300 || status >= 400);
    public void OnResultExecuted(ResultExecutedContext context) { }
}
