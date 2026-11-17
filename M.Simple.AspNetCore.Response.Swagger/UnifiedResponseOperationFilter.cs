using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace M.Simple.AspNetCore.Response.Swagger;

internal sealed class UnifiedResponseOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<SkipUnifiedResponseAttribute>().Any()) return;
        foreach (var entry in operation.Responses.ToArray())
        {
            if (!int.TryParse(entry.Key, out var status) || status < 200 || status is >= 300 and < 400) continue;
            var responseType = context.ApiDescription.SupportedResponseTypes.FirstOrDefault(r => r.StatusCode == status)?.Type;
            if (responseType is not null && (typeof(FileResult).IsAssignableFrom(responseType)
                || typeof(ContentResult).IsAssignableFrom(responseType))) continue;
            if (responseType is not null && typeof(IApiResponse).IsAssignableFrom(responseType)) continue;
            var jsonContent = entry.Value.Content.Where(c => c.Key.Contains("json", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (jsonContent.Length == 0 && entry.Value.Content.Count > 0) continue;
            if (jsonContent.Length == 0)
                entry.Value.Content["application/json"] = new OpenApiMediaType { Schema = Wrap(null) };
            else
            {
                foreach (var content in jsonContent)
                {
                    var data = responseType is not null && typeof(ProblemDetails).IsAssignableFrom(responseType)
                        ? typeof(ValidationProblemDetails).IsAssignableFrom(responseType) ? ValidationErrors() : null
                        : content.Value.Schema;
                    content.Value.Schema = Wrap(data);
                }
                entry.Value.Content.Remove("text/plain");
            }
            if (status == 204)
            {
                operation.Responses.Remove(entry.Key);
                operation.Responses.TryAdd("200", entry.Value);
            }
        }
        if (!operation.Responses.ContainsKey("400"))
            operation.Responses["400"] = JsonResponse("请求参数错误或参数校验失败", ValidationErrors());
        if (!operation.Responses.ContainsKey("500"))
            operation.Responses["500"] = JsonResponse("服务器内部错误", null);
    }

    private static OpenApiResponse JsonResponse(string description, OpenApiSchema? data) => new()
    {
        Description = description,
        Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new() { Schema = Wrap(data) } }
    };
    private static OpenApiSchema ValidationErrors() => new()
    {
        Type = "object",
        AdditionalProperties = new() { Type = "array", Items = new() { Type = "string" } }
    };
    private static OpenApiSchema Wrap(OpenApiSchema? data) => new()
    {
        Type = "object",
        Required = new HashSet<string> { "code", "message", "data" },
        Properties = new Dictionary<string, OpenApiSchema>
        {
            ["code"] = new() { Type = "integer", Format = "int32" },
            ["message"] = new() { Type = "string" },
            ["data"] = data is null ? new() { Nullable = true }
                : new() { Nullable = true, AllOf = new List<OpenApiSchema> { data } }
        }
    };
}
