using System.Text.Json.Serialization;

namespace M.Simple.AspNetCore.Response;

/// <summary>Marks an already wrapped response.</summary>
public interface IApiResponse;

/// <summary>A unified JSON response. Code defaults to the HTTP status code.</summary>
public sealed record ApiResponse<T>(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data"), JsonIgnore(Condition = JsonIgnoreCondition.Never)] T? Data) : IApiResponse;

/// <summary>Opts a controller or action out of automatic response wrapping.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class SkipUnifiedResponseAttribute : Attribute;
