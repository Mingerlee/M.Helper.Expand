namespace M.Simple.AspNetCore.Response;

public sealed class UnifiedResponseOptions
{
    public string SuccessMessage { get; set; } = "成功";
    public string ValidationErrorMessage { get; set; } = "参数校验失败";
    public string ExceptionMessage { get; set; } = "服务器内部错误";

    /// <summary>Overrides status messages. Return null to use the default message.</summary>
    public Func<int, string?>? StatusMessageFactory { get; set; }

    public string GetMessage(int statusCode) => StatusMessageFactory?.Invoke(statusCode) ?? statusCode switch
    {
        >= 200 and < 300 => SuccessMessage,
        400 => "请求参数错误",
        401 => "未登录或登录已过期",
        403 => "没有访问权限",
        404 => "资源不存在",
        405 => "请求方法不允许",
        409 => "请求与当前资源状态冲突",
        >= 500 => ExceptionMessage,
        _ => "请求失败"
    };

    internal ApiResponse<object?> Create(int statusCode, object? data = null, string? message = null)
        => new(statusCode, message ?? GetMessage(statusCode), data);
}
