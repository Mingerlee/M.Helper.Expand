using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace M.Simple.Logging;

public static class SimpleLoggingExtensions
{
    /// <summary>注册日志采集和后台写入器。应在宿主构建前调用一次。</summary>
    public static IServiceCollection AddSimpleLogging(this IServiceCollection services,
        Action<SimpleLoggingOptions>? configure = null)
    {
        if (services.Any(s => s.ServiceType == typeof(LogBuffer)))
            throw new InvalidOperationException("AddSimpleLogging 只能注册一次。");
        var options = new SimpleLoggingOptions();
        configure?.Invoke(options);
        options.Validate();
        services.AddSingleton(options);
        services.AddSingleton(sp => new LogBuffer(sp.GetRequiredService<SimpleLoggingOptions>()));
        services.AddSingleton(sp => new SqliteLogStore(sp.GetRequiredService<SimpleLoggingOptions>(), sp.GetRequiredService<LogBuffer>()));
        services.AddSingleton(sp => new SimpleLoggerProvider(sp.GetRequiredService<SimpleLoggingOptions>(), sp.GetRequiredService<LogBuffer>()));
        services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<SimpleLoggerProvider>());
        services.TryAddSingleton<IDiskSpaceProbe, DiskSpaceProbe>();
        services.AddSingleton<StorageGuard>();
        services.AddSingleton<IHostedService, LogWriter>();
        // Dashboard 自身轮询产生的框架请求日志默认不采集，仍保留框架警告和异常。
        services.AddLogging(logging => logging.AddFilter<SimpleLoggerProvider>("Microsoft.AspNetCore", LogLevel.Warning));
        services.AddAuthorization();
        return services;
    }

    /// <summary>挂载内嵌页面与只读 API。默认要求授权，可在返回的路由组上指定策略。</summary>
#if NET6_0
    public static SimpleLogDashboardBuilder MapSimpleLogDashboard(this IEndpointRouteBuilder endpoints, string path = "/logs")
#else
    public static RouteGroupBuilder MapSimpleLogDashboard(this IEndpointRouteBuilder endpoints, string path = "/logs")
#endif
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/') || path == "/" ||
            path.Contains('{') || path.Contains('?') || path.Contains('#'))
            throw new ArgumentException("Dashboard 路径必须是非根目录的固定绝对路径。", nameof(path));
#if NET6_0
        return MapNet6Dashboard(endpoints, path.TrimEnd('/'));
#else
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
#endif
    }

#if NET6_0
    private static SimpleLogDashboardBuilder MapNet6Dashboard(IEndpointRouteBuilder endpoints, string path)
    {
        var builder = new SimpleLogDashboardBuilder();
        void Get(string suffix, Func<HttpContext, SqliteLogStore, Task<IResult>> action)
        {
            var endpoint = endpoints.MapGet(path + suffix, (RequestDelegate)(async context =>
            {
                var headers = context.Response.Headers;
                headers["Cache-Control"] = "no-store";
                headers["X-Content-Type-Options"] = "nosniff";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Content-Security-Policy"] =
                    "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
                IResult result;
                try { result = await action(context, context.RequestServices.GetRequiredService<SqliteLogStore>()); }
                catch (ArgumentException) { result = Results.BadRequest(new { error = "查询参数无效。" }); }
                catch (Microsoft.Data.Sqlite.SqliteException)
                {
                    result = Results.Problem("日志数据库暂时不可用，请稍后重试。", statusCode: 503);
                }
                await result.ExecuteAsync(context);
            }));
            endpoint.RequireAuthorization();
            builder.AddEndpoint(endpoint);
        }
        Get("/", (_, _) => Task.FromResult(Results.Content(DashboardHtml.Value, "text/html; charset=utf-8")));
        Get("/api/logs", async (context, store) =>
        {
            var request = context.Request;
            return Results.Ok(await store.QueryAsync(new LogQuery
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
            }, context.RequestAborted));
        });
        Get("/api/logs/{id:long}", async (context, store) =>
            await store.GetAsync(Convert.ToInt64(context.Request.RouteValues["id"], CultureInfo.InvariantCulture), context.RequestAborted)
                is { } entry ? Results.Ok(entry) : Results.NotFound());
        Get("/api/statistics", async (context, store) => Results.Ok(await store.GetStatisticsAsync(
            Number(context.Request, "utcOffsetMinutes") ?? 0, context.RequestAborted)));
        Get("/api/status", (_, store) => Task.FromResult(Results.Ok(store.GetStatus())));
        return builder;
    }
#endif

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
        using var stream = typeof(SimpleLoggingExtensions).Assembly.GetManifestResourceStream("M.Simple.Logging.Dashboard.index.html")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });
}
