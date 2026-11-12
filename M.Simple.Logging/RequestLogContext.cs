using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace M.Simple.Logging;

internal sealed class RequestLogContext(HttpContext http, SimpleRequestLoggingOptions options)
{
    private static readonly AsyncLocal<Holder?> CurrentHolder = new();
    private object? _body;
    private string _bodyStatus = "NotCaptured";
    private bool _truncated;
    private readonly HashSet<string> _sensitive = options.SensitiveFields.Select(Normalize).ToHashSet(StringComparer.Ordinal);

    internal static RequestLogContext? Current => CurrentHolder.Value?.Context;
    internal bool IncludeInErrorLogs => options.IncludeInErrorLogs;
    internal static IDisposable Push(RequestLogContext context)
    {
        var previous = CurrentHolder.Value;
        var holder = new Holder { Context = context };
        CurrentHolder.Value = holder;
        return new Lease(() =>
        {
            // 清空子任务共享的 holder，避免后台任务保留请求对象或污染后续日志。
            holder.Context = null;
            CurrentHolder.Value = previous;
        });
    }

    internal async Task CaptureBodyAsync()
    {
        var request = http.Request;
        if (!options.IncludeBody) { _bodyStatus = "Disabled"; return; }
        var contentType = request.ContentType?.Split(';')[0].Trim().ToLowerInvariant() ?? "";
        if (contentType == "multipart/form-data") { _bodyStatus = "FormNotParsed"; return; }
        var json = contentType == "application/json" || contentType.EndsWith("+json", StringComparison.Ordinal);
        var form = contentType == "application/x-www-form-urlencoded";
        if (!json && !form) { _bodyStatus = "UnsupportedContentType"; return; }
        if (request.ContentLength > options.MaxBodyBytes) { _bodyStatus = "TooLarge"; return; }
        request.EnableBuffering();
        var position = request.Body.Position;
        try
        {
            var bytes = new byte[options.MaxBodyBytes + 1];
            var length = 0;
            while (length < bytes.Length)
            {
                var count = await request.Body.ReadAsync(bytes.AsMemory(length), http.RequestAborted);
                if (count == 0) break;
                length += count;
            }
            if (length > options.MaxBodyBytes) { _bodyStatus = "TooLarge"; return; }
            if (length == 0) { _bodyStatus = "Empty"; return; }
            if (json)
            {
                // 不保存原始文本；无效 JSON 或超深内容直接跳过，避免截断后泄露敏感字段。
                using var document = JsonDocument.Parse(bytes.AsMemory(0, length), new JsonDocumentOptions { MaxDepth = 32 });
                _body = Sanitize(document.RootElement);
            }
            else
            {
                using var stream = new MemoryStream(bytes, 0, length);
                using var reader = new FormReader(stream);
                _body = Parameters(await reader.ReadFormAsync(http.RequestAborted));
            }
            _bodyStatus = "Captured";
        }
        catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception) { _body = null; _bodyStatus = "UnavailableOrInvalid"; }
        finally { request.Body.Position = position; }
    }

    internal object Snapshot()
    {
        try
        {
            object? body = _body;
            var status = _bodyStatus;
            if (status == "FormNotParsed" && http.Features.Get<IFormFeature>()?.Form is { } form)
            {
                // 只使用下游已经解析的表单，绝不为了记录日志读取上传文件流。
                body = new
                {
                    Fields = Parameters(form),
                    Files = form.Files.Take(100).Select(file => new
                    {
                        Field = Limit(file.Name),
                        FileName = IsSensitive(file.Name) ? "[REDACTED]" : Limit(file.FileName),
                        file.Length
                    }).ToArray()
                };
                status = "CapturedFormMetadata";
            }
            var snapshot = new Dictionary<string, object?>
            {
                ["Method"] = http.Request.Method,
                ["Path"] = Limit(http.Request.PathBase + http.Request.Path),
                ["Query"] = Parameters(http.Request.Query),
                ["RouteValues"] = http.Request.RouteValues.Take(100).ToDictionary(p => p.Key,
                    p => IsSensitive(p.Key) ? "[REDACTED]" : Limit(p.Value?.ToString() ?? "")),
                ["RequestId"] = http.TraceIdentifier,
                ["TraceId"] = Activity.Current?.TraceId.ToString(),
                ["Body"] = body,
                ["BodyStatus"] = status,
                ["Truncated"] = _truncated
            };
            if (JsonSerializer.SerializeToUtf8Bytes(snapshot).Length > options.MaxBodyBytes)
            {
                snapshot["Body"] = null;
                snapshot["BodyStatus"] = "ContextTooLarge";
                snapshot["Query"] = "[OMITTED: context limit]";
                snapshot["RouteValues"] = "[OMITTED: context limit]";
                snapshot["Path"] = http.Request.Path.Value?.Length > 128 ? "[OMITTED: path limit]" : http.Request.Path.Value;
                snapshot["Truncated"] = true;
                if (JsonSerializer.SerializeToUtf8Bytes(snapshot).Length > options.MaxBodyBytes)
                    return new { BodyStatus = "ContextTooLarge", Truncated = true };
            }
            return snapshot;
        }
        catch (Exception) { return new { BodyStatus = "SnapshotUnavailable" }; }
    }

    private Dictionary<string, object?> Parameters(IEnumerable<KeyValuePair<string, StringValues>> values)
    {
        var result = new Dictionary<string, object?>();
        foreach (var pair in values.Take(101))
        {
            if (result.Count == 100) { _truncated = true; break; }
            result[Limit(pair.Key)] = IsSensitive(pair.Key) ? "[REDACTED]" : pair.Value.Take(20).Select(s => Limit(s ?? "")).ToArray();
            if (pair.Value.Count > 20) _truncated = true;
        }
        return result;
    }

    private object? Sanitize(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => SanitizeObject(value),
        JsonValueKind.Array => SanitizeArray(value),
        JsonValueKind.String => Limit(value.GetString() ?? ""),
        _ => value.Clone()
    };

    private Dictionary<string, object?> SanitizeObject(JsonElement value)
    {
        var result = new Dictionary<string, object?>();
        foreach (var property in value.EnumerateObject().Take(101))
        {
            if (result.Count == 100) { _truncated = true; break; }
            result[Limit(property.Name)] = IsSensitive(property.Name) ? "[REDACTED]" : Sanitize(property.Value);
        }
        return result;
    }

    private object?[] SanitizeArray(JsonElement value)
    {
        if (value.GetArrayLength() > 100) _truncated = true;
        return value.EnumerateArray().Take(100).Select(Sanitize).ToArray();
    }

    private bool IsSensitive(string key)
    {
        var normalized = Normalize(key);
        return _sensitive.Contains(normalized) || new[] { "password", "passwd", "pwd", "token", "secret", "authorization", "cookie", "apikey" }
            .Any(part => normalized.Contains(part, StringComparison.Ordinal));
    }

    private static string Normalize(string value) => string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();
    private string Limit(string value)
    {
        if (value.Length <= 1024) return value;
        _truncated = true;
        return value[..1024] + " [truncated]";
    }

    private sealed class Holder { internal RequestLogContext? Context; }
    private sealed class Lease(Action dispose) : IDisposable { public void Dispose() => dispose(); }
}
