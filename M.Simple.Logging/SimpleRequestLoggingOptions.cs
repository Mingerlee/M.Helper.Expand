using Microsoft.AspNetCore.Http;

namespace M.Simple.Logging;

/// <summary>错误日志的请求参数采集配置。只有安装请求中间件后生效。</summary>
public sealed class SimpleRequestLoggingOptions
{
    /// <summary>请求体读取上限及请求上下文 JSON 字节预算，默认 16 KiB。</summary>
    public int MaxBodyBytes { get; set; } = 16 * 1024;
    public bool IncludeBody { get; set; } = true;
    /// <summary>普通 Error/Critical 日志是否默认附带请求信息；可由单条 LogError 的重载覆盖。</summary>
    public bool IncludeInErrorLogs { get; set; } = true;
    public bool LogUnhandledExceptions { get; set; } = true;
    /// <summary>可选路径筛选，例如只采集 /api；返回 false 时完全跳过。</summary>
    public Func<HttpContext, bool>? ShouldCapture { get; set; }
    /// <summary>追加需要脱敏的字段名称，不区分大小写，忽略标点后匹配。</summary>
    public ISet<string> SensitiveFields { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    internal void Validate()
    {
        if (MaxBodyBytes is < 256 or > 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(MaxBodyBytes), "请求体上限必须在 256 字节至 1 MiB 之间。");
    }
}
