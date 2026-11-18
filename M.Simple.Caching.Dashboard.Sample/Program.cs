using Microsoft.AspNetCore.DataProtection;
using System.Net;
using System.Security.Claims;
using M.Simple.Caching;
using M.Simple.Caching.Dashboard;
using M.Simple.Caching.MongoDB;
using M.Simple.Caching.Redis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("This sample's demonstration login is available only in Development. Set ASPNETCORE_ENVIRONMENT=Development.");
// Local demonstration only: production applications must use their own authenticated users.
builder.WebHost.UseUrls("http://127.0.0.1:5189");
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o => o.LoginPath = "/demo/login");
builder.Services.AddAuthorization(o => o.AddPolicy("CacheAdmin", p => p.RequireRole("CacheAdmin")));
switch (builder.Configuration["backend"]?.ToLowerInvariant() ?? "memory")
{
    case "memory":
        builder.Services.AddSimpleMemoryCache(o => o.KeyPrefix = "CacheDashboardSample:");
        break;
    case "redis":
        builder.Services.AddSimpleRedisCache(o =>
        {
            o.KeyPrefix = "CacheDashboardSample:";
            o.ConnectionString = Environment.GetEnvironmentVariable("M_SIMPLE_REDIS") ?? "127.0.0.1:6379";
        });
        break;
    case "mongo":
        builder.Services.AddSimpleMongoCache(o =>
        {
            o.KeyPrefix = "CacheDashboardSample:";
            o.ConnectionString = Environment.GetEnvironmentVariable("M_SIMPLE_MONGODB") ?? "mongodb://127.0.0.1:27017/?directConnection=true";
            o.DatabaseName = "M_Simple_Cache_Dashboard_Sample";
        });
        break;
    default: throw new ArgumentException("Choose --backend memory, redis or mongo.");
}
builder.Services.AddSimpleCacheDashboard(o => o.ReadOnly = builder.Configuration.GetValue<bool>("readonly"));
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapSimpleCacheDashboard("/cache").RequireAuthorization("CacheAdmin");
app.MapGet("/", () => Results.Redirect("/cache/"));
app.MapGet("/demo/login", (HttpContext http, IAntiforgery antiforgery) =>
{
    var tokens = antiforgery.GetAndStoreTokens(http);
    var html = """
        <!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><title>缓存管理开发示例</title></head>
        <body><h1>缓存管理开发示例</h1><p>此入口仅供本机 Development 示例使用；业务项目请使用自己的认证授权。</p>
        <form method="post"><input type="hidden" name="{FIELD}" value="{TOKEN}">
        <button type="submit">以演示管理员进入</button></form></body></html>
        """;
    http.Response.Headers.CacheControl = "no-store";
    return Results.Content(html.Replace("{FIELD}", WebUtility.HtmlEncode(tokens.FormFieldName))
        .Replace("{TOKEN}", WebUtility.HtmlEncode(tokens.RequestToken)), "text/html; charset=utf-8");
});
app.MapPost("/demo/login", async (HttpContext http, IAntiforgery antiforgery) =>
{
    try { await antiforgery.ValidateRequestAsync(http); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "demo-admin"),
        new Claim(ClaimTypes.Name, "演示管理员"), new Claim(ClaimTypes.Role, "CacheAdmin") }, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Redirect("/cache/");
});

var cache = app.Services.GetRequiredService<ISimpleCache>();
for (var i = 1; i <= 12; i++)
    await cache.SetAsync("product:" + i.ToString("D3"), new { Id = i, Name = "示例商品 " + i, Price = i * 12.5m });
await cache.SetAsync("sliding:example", new { Description = "查看详情不会续期" },
    new() { SlidingExpiration = TimeSpan.FromMinutes(2), AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });
await cache.SetAsync<string?>("null:example", null);
await cache.GetOrCreateAsync("factory:example", _ => Task.FromResult(new { Source = "工厂加载" }));
await cache.TryGetAsync<int>("missing");
await cache.TryGetAsync<object>("not-created");
await app.RunAsync();
