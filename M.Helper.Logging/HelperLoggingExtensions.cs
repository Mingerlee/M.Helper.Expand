using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace M.Helper.Logging;

public static class HelperLoggingExtensions
{
    /// <summary>注册日志采集和后台写入器。应在宿主构建前调用一次。</summary>
    public static IServiceCollection AddHelperLogging(this IServiceCollection services,
        Action<HelperLoggingOptions>? configure = null)
    {
        if (services.Any(s => s.ServiceType == typeof(LogBuffer)))
            throw new InvalidOperationException("AddHelperLogging 只能注册一次。");
        var options = new HelperLoggingOptions();
        configure?.Invoke(options);
        options.Validate();
        services.AddSingleton(options);
        services.AddSingleton(sp => new LogBuffer(sp.GetRequiredService<HelperLoggingOptions>()));
        services.AddSingleton(sp => new SqliteLogStore(sp.GetRequiredService<HelperLoggingOptions>(), sp.GetRequiredService<LogBuffer>()));
        services.AddSingleton(sp => new HelperLoggerProvider(sp.GetRequiredService<HelperLoggingOptions>(), sp.GetRequiredService<LogBuffer>()));
        services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<HelperLoggerProvider>());
        services.TryAddSingleton<IDiskSpaceProbe, DiskSpaceProbe>();
        services.AddSingleton<StorageGuard>();
        services.AddSingleton<IHostedService, LogWriter>();
        // Dashboard 自身轮询产生的框架请求日志默认不采集，仍保留框架警告和异常。
        services.AddLogging(logging => logging.AddFilter<HelperLoggerProvider>("Microsoft.AspNetCore", LogLevel.Warning));
        services.AddAuthorization();
        return services;
    }

    /// <summary>挂载内嵌页面与只读 API。默认要求授权，可在返回的路由组上指定策略。</summary>
    public static RouteGroupBuilder MapHelperLogDashboard(this IEndpointRouteBuilder endpoints, string path = "/logs")
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/') || path == "/" ||
            path.Contains('{') || path.Contains('?') || path.Contains('#'))
            throw new ArgumentException("Dashboard 路径必须是非根目录的固定绝对路径。", nameof(path));
        var group = endpoints.MapGroup(path.TrimEnd('/'));
        group.RequireAuthorization();
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            context.HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.HttpContext.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.HttpContext.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
            try { return await next(context); }
            catch (ArgumentException) { return Results.BadRequest(new { error = "查询参数无效。" }); }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                return Results.Problem("日志数据库暂时不可用，请稍后重试。", statusCode: 503);
            }
        });
        group.MapGet("/", () => Results.Content(DashboardHtml.Value, "text/html; charset=utf-8"));
        group.MapGet("/api/logs", async (HttpRequest request, SqliteLogStore store, CancellationToken ct) =>
        {
            var query = new LogQuery
            {
                From = Date(request, "from"),
                To = Date(request, "to"),
                Level = Number(request, "level"),
                MinimumLevel = Number(request, "minimumLevel"),
                Category = request.Query["category"].ToString(),
                Keyword = request.Query["keyword"].ToString(),
                TraceId = request.Query["traceId"].ToString(),
                BeforeId = Long(request, "beforeId"),
                PageSize = Number(request, "pageSize") ?? 50
            };
            return Results.Ok(await store.QueryAsync(query, ct));
        });
        group.MapGet("/api/logs/{id:long}", async (long id, SqliteLogStore store, CancellationToken ct) =>
            await store.GetAsync(id, ct) is { } entry ? Results.Ok(entry) : Results.NotFound());
        group.MapGet("/api/statistics", async (HttpRequest request, SqliteLogStore store, CancellationToken ct) =>
            Results.Ok(await store.GetStatisticsAsync(Number(request, "utcOffsetMinutes") ?? 0, ct)));
        group.MapGet("/api/status", (SqliteLogStore store) => Results.Ok(store.GetStatus()));
        return group;
    }

    private static string? Value(HttpRequest request, string name)
    {
        var value = request.Query[name];
        if (value.Count > 1) throw new ArgumentException("参数不可重复。");
        return string.IsNullOrWhiteSpace(value.ToString()) ? null : value.ToString();
    }

    private static int? Number(HttpRequest request, string name) => Value(request, name) is { } text
        ? int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value : throw new ArgumentException("无效数字。") : null;

    private static long? Long(HttpRequest request, string name) => Value(request, name) is { } text
        ? long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value : throw new ArgumentException("无效游标。") : null;

    private static DateTimeOffset? Date(HttpRequest request, string name) => Value(request, name) is { } text
        ? DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value)
            ? value : throw new ArgumentException("无效时间。") : null;

    private static readonly Lazy<string> DashboardHtml = new(() =>
    {
        using var stream = typeof(HelperLoggingExtensions).Assembly.GetManifestResourceStream("M.Helper.Logging.Dashboard.index.html")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });
}
