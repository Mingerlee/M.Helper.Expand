using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace M.Simple.Caching.Dashboard;

public static class SimpleCacheDashboardExtensions
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static IServiceCollection AddSimpleCacheDashboard(this IServiceCollection services, Action<SimpleCacheDashboardOptions>? configure = null)
    {
        if (services.Any(s => s.ServiceType == typeof(SimpleCacheDashboardOptions)))
            throw new InvalidOperationException("Register the cache dashboard only once.");
        var options = new SimpleCacheDashboardOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<DashboardState>();
        services.AddAuthorization();
        services.AddAntiforgery();
        services.AddLogging();
        return services;
    }

    public static RouteGroupBuilder MapSimpleCacheDashboard(this IEndpointRouteBuilder endpoints, string path = "/cache")
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/') || path.Trim('/') == "" ||
            path.Contains("//") || path.IndexOfAny(new[] { '{', '}', '?', '#', '*', '\\' }) >= 0)
            throw new ArgumentException("Dashboard requires a fixed non-root absolute path.", nameof(path));
        _ = endpoints.ServiceProvider.GetRequiredService<SimpleCacheDashboardOptions>();
        _ = endpoints.ServiceProvider.GetRequiredService<ISimpleCacheAdmin>();
        var group = endpoints.MapGroup(path.TrimEnd('/'));
        group.RequireAuthorization();
        group.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            http.Response.Headers.CacheControl = "no-cache, no-store";
            http.Response.Headers.Pragma = "no-cache";
            http.Response.Headers["X-Content-Type-Options"] = "nosniff";
            http.Response.Headers["Referrer-Policy"] = "no-referrer";
            http.Response.Headers["Content-Security-Policy"] = "default-src 'none'; script-src 'self'; style-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
            if (http.User.Identity?.IsAuthenticated != true) return Results.Unauthorized();
            try
            {
                if (HttpMethods.IsPost(http.Request.Method))
                {
                    if (http.RequestServices.GetRequiredService<SimpleCacheDashboardOptions>().ReadOnly)
                        return Results.StatusCode(StatusCodes.Status403Forbidden);
                    await http.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(http);
                }
                return await next(context);
            }
            catch (AntiforgeryValidationException) { return Results.BadRequest(new { error = "防伪令牌无效，请刷新页面。" }); }
            catch (ArgumentException) { return Results.BadRequest(new { error = "参数或游标无效，请重新查询。" }); }
            catch (JsonException) { return Results.BadRequest(new { error = "请求 JSON 无效。" }); }
            catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested) { return Results.StatusCode(499); }
            catch (Exception error)
            {
                http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("M.Simple.Caching.Dashboard")
                    .LogError(error, "Cache dashboard request failed. TraceId={TraceId}", http.TraceIdentifier);
                return Results.Problem("缓存服务暂时不可用，请稍后重试。", statusCode: 503);
            }
        });

        group.MapGet("/", (HttpContext http) => http.Request.Path.Value!.EndsWith('/')
            ? Results.Content(Resource("index.html"), "text/html; charset=utf-8")
            : Results.Redirect(http.Request.PathBase + http.Request.Path + "/"));
        group.MapGet("/dashboard.js", () => Results.Content(Resource("dashboard.js"), "text/javascript; charset=utf-8"));
        group.MapGet("/dashboard.css", () => Results.Content(Resource("dashboard.css"), "text/css; charset=utf-8"));
        group.MapGet("/api/token", (HttpContext http, IAntiforgery antiforgery, IOptions<AntiforgeryOptions> options) =>
        {
            var header = options.Value.HeaderName;
            if (string.IsNullOrEmpty(header)) throw new InvalidOperationException("Antiforgery HeaderName must be configured.");
            return Results.Ok(new { token = antiforgery.GetAndStoreTokens(http).RequestToken, header });
        });
        group.MapGet("/api/status", (ISimpleCacheAdmin admin, SimpleCacheDashboardOptions options) =>
            Results.Ok(new { admin.Backend, admin.KeyPrefix, options.ReadOnly, Instance = Environment.MachineName + ":" + Environment.ProcessId,
                Statistics = admin.GetStatistics() }));
        group.MapGet("/api/entries", async (HttpContext http, ISimpleCacheAdmin admin) =>
        {
            var sizeText = Value(http.Request, "pageSize");
            var size = sizeText is null ? 50 : int.TryParse(sizeText, out var parsed) ? parsed : throw new ArgumentException();
            return Results.Ok(await admin.ScanAsync(Value(http.Request, "prefix") ?? "", Value(http.Request, "cursor"), size, http.RequestAborted));
        });
        group.MapGet("/api/entry", async (HttpContext http, ISimpleCacheAdmin admin) =>
            await admin.PeekAsync(Value(http.Request, "key") ?? "", http.RequestAborted) is { } item ? Results.Ok(item) : Results.NotFound());
        group.MapGet("/api/operations", (DashboardState state) => Results.Ok(state.Operations()));

        group.MapPost("/api/remove", async (HttpContext http, ISimpleCacheAdmin admin, DashboardState state) =>
        {
            var body = await Body<RemoveRequest>(http);
            var keys = ValidateKeys(body.Keys, admin);
            var removed = await Delete(http, admin, state, keys, "remove");
            return Results.Ok(new { requested = keys.Length, removed });
        });
        group.MapPost("/api/preview", async (HttpContext http, ISimpleCacheAdmin admin, DashboardState state) =>
        {
            var body = await Body<PreviewRequest>(http);
            if (body.Prefix is null) throw new ArgumentException("Prefix is required.");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            string? cursor = null;
            // Bounded scan; a preview may be partial. The exact listed keys are the deletion scope.
            for (var i = 0; i < 20; i++)
            {
                var page = await admin.ScanAsync(body.Prefix, cursor, Math.Min(100, 200 - keys.Count), http.RequestAborted);
                foreach (var entry in page.Items) keys.Add(entry.Key);
                cursor = page.NextCursor;
                if (cursor is null || keys.Count >= 200) break;
            }
            var token = state.CreatePlan(http.User, body.Prefix, keys.ToArray());
            return Results.Ok(new { token, keys = keys.ToArray(), partial = cursor is not null, expiresInSeconds = 120 });
        });
        group.MapPost("/api/clear", async (HttpContext http, ISimpleCacheAdmin admin, DashboardState state) =>
        {
            var body = await Body<ClearRequest>(http);
            if (string.IsNullOrEmpty(body.Token) || body.Token.Length > 128) throw new ArgumentException();
            var plan = state.TakePlan(body.Token, http.User);
            if (plan is null) return Results.Conflict(new { error = "预览已过期、已使用或属于其他用户，请重新预览。" });
            var removed = await Delete(http, admin, state, plan.Keys, "prefix-remove");
            return Results.Ok(new { requested = plan.Keys.Length, removed });
        });
        return group;
    }

    private static string[] ValidateKeys(string[]? keys, ISimpleCacheAdmin admin)
    {
        if (keys is null || keys.Length is < 1 or > 200 ||
            keys.Any(k => string.IsNullOrWhiteSpace(k) || k.Length + admin.KeyPrefix.Length > 1024)) throw new ArgumentException();
        return keys.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static async Task<int> Delete(HttpContext http, ISimpleCacheAdmin admin, DashboardState state, string[] keys, string action)
    {
        var removed = 0;
        foreach (var key in keys)
        {
            var outcome = "failed";
            try
            {
                var found = await admin.RemoveAsync(key, http.RequestAborted);
                if (found) removed++;
                outcome = found ? "removed" : "absent";
            }
            finally { state.Audit(http.User, action, key, outcome); }
        }
        return removed;
    }

    private static string? Value(HttpRequest request, string name)
    {
        var values = request.Query[name];
        if (values.Count > 1) throw new ArgumentException();
        return values.Count == 0 || values[0] == "" ? null : values[0];
    }

    private static async Task<T> Body<T>(HttpContext http)
    {
        if (http.Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) != true)
            throw new ArgumentException("JSON content type required.");
        using var stream = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await http.Request.Body.ReadAsync(buffer, http.RequestAborted)) > 0)
        {
            if (stream.Length + read > 256 * 1024) throw new ArgumentException("Request is too large.");
            stream.Write(buffer, 0, read);
        }
        return JsonSerializer.Deserialize<T>(stream.ToArray(), Json) ?? throw new ArgumentException();
    }

    private static string Resource(string name)
    {
        using var stream = typeof(SimpleCacheDashboardExtensions).Assembly.GetManifestResourceStream("M.Simple.Caching.Dashboard.Dashboard." + name)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private sealed record RemoveRequest(string[]? Keys);
    private sealed record PreviewRequest(string? Prefix);
    private sealed record ClearRequest(string? Token);
}
