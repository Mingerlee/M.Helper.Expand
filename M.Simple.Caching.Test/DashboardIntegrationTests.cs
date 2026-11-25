using Microsoft.AspNetCore.DataProtection;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using M.Simple.Caching.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Caching.Test;

[TestClass]
public sealed class DashboardIntegrationTests
{
    private readonly ManualClock _clock = new();
    private WebApplication _app = null!;
    private HttpClient _http = null!;
    private ISimpleCache Cache => _app.Services.GetRequiredService<ISimpleCache>();

    [TestInitialize]
    public Task Start() => StartHost();

    private async Task StartHost(Action<SimpleCacheDashboardOptions>? configure = null, bool allowAnonymous = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("test", _ => { });
        builder.Services.AddAuthorization(o => o.AddPolicy("CacheAdmin", p => p.RequireRole("admin")));
        builder.Services.AddSingleton<TimeProvider>(_clock);
        builder.Services.AddSimpleMemoryCache(o => o.KeyPrefix = "dashboard-test:");
        builder.Services.AddSimpleCacheDashboard(configure);
        _app = builder.Build();
        _app.UsePathBase("/tools");
        _app.UseAuthentication();
        _app.UseAuthorization();
        var dashboard = _app.MapSimpleCacheDashboard("/cache").RequireAuthorization("CacheAdmin");
        if (allowAnonymous) dashboard.AllowAnonymous();
        await _app.StartAsync();
        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        _http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(address + "/tools/cache/") };
        _http.DefaultRequestHeaders.Add("X-Test-User", "alice");
    }

    [TestCleanup]
    public async Task Stop()
    {
        _http.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private async Task<JsonElement> Get(string path)
    {
        using var response = await _http.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).Clone();
    }

    private async Task<HttpResponseMessage> Post(string path, object body)
    {
        var token = await Get("api/token");
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add(token.GetProperty("header").GetString()!, token.GetProperty("token").GetString());
        return await _http.SendAsync(request);
    }

    [TestMethod]
    public async Task ExplicitAllowAnonymousOpensPageAssetsAndReadApisButKeepsReadOnlyProtection()
    {
        await Stop();
        await StartHost(o => o.ReadOnly = true, allowAnonymous: true);
        _http.DefaultRequestHeaders.Remove("X-Test-User");
        await Cache.SetAsync("a", 1);
        foreach (var path in new[] { "", "dashboard.js", "dashboard.css", "api/token", "api/status", "api/entries", "api/entry?key=a", "api/operations" })
        {
            using var response = await _http.GetAsync(path);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, path);
        }
        foreach (var path in new[] { "api/remove", "api/preview", "api/clear" })
        {
            using var response = await _http.PostAsJsonAsync(path, new { keys = new[] { "a" } });
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, path);
        }
        Assert.IsTrue(await Cache.ExistsAsync("a"));
    }

    [TestMethod]
    public async Task EveryPageAssetAndApiRequiresAuthenticationAndConfiguredRole()
    {
        foreach (var user in new[] { "", "reader" })
        {
            _http.DefaultRequestHeaders.Remove("X-Test-User");
            if (user != "") _http.DefaultRequestHeaders.Add("X-Test-User", user);
            _http.DefaultRequestHeaders.Remove("X-Test-Role");
            _http.DefaultRequestHeaders.Add("X-Test-Role", "reader");
            foreach (var path in new[] { "", "dashboard.js", "dashboard.css", "api/token", "api/status", "api/entries", "api/entry?key=a", "api/operations" })
            {
                using var response = await _http.GetAsync(path);
                Assert.AreEqual(user == "" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, response.StatusCode, path);
            }
            foreach (var path in new[] { "api/remove", "api/preview", "api/clear" })
            {
                using var response = await _http.PostAsJsonAsync(path, new { keys = new[] { "a" } });
                Assert.AreEqual(user == "" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, response.StatusCode, path);
            }
        }
    }

    [TestMethod]
    public async Task EmbeddedAssetsAndApisWorkWithPathBaseAndNeverExposeValueInListOrAudit()
    {
        await Cache.SetAsync("product:1", new { Secret = "value-only-in-detail", Name = "<script>alert(1)</script>" });
        using var page = await _http.GetAsync("");
        Assert.AreEqual(HttpStatusCode.OK, page.StatusCode);
        StringAssert.Contains(await page.Content.ReadAsStringAsync(), "./dashboard.js");
        Assert.IsTrue(page.Headers.CacheControl!.NoStore);
        StringAssert.Contains(page.Headers.GetValues("Content-Security-Policy").Single(), "frame-ancestors 'none'");
        foreach (var asset in new[] { "dashboard.js", "dashboard.css" })
        {
            using var response = await _http.GetAsync(asset);
            response.EnsureSuccessStatusCode();
        }
        using var redirect = await _http.GetAsync(_http.BaseAddress!.AbsoluteUri.TrimEnd('/'));
        Assert.AreEqual(HttpStatusCode.Redirect, redirect.StatusCode);
        Assert.AreEqual("/tools/cache/", redirect.Headers.Location!.ToString());
        var entries = await Get("api/entries?prefix=product:");
        Assert.AreEqual(1, entries.GetProperty("items").GetArrayLength());
        Assert.IsFalse(entries.ToString().Contains("value-only-in-detail"));
        var detail = await Get("api/entry?key=product%3A1");
        StringAssert.Contains(detail.GetProperty("json").GetString()!, "value-only-in-detail");
        var status = await Get("api/status");
        Assert.AreEqual("Memory", status.GetProperty("backend").GetString());
        Assert.AreEqual(0L, status.GetProperty("statistics").GetProperty("hits").GetInt64());
        using var removed = await Post("api/remove", new { keys = new[] { "product:1" } });
        removed.EnsureSuccessStatusCode();
        var operations = await Get("api/operations");
        Assert.AreEqual("alice", operations[0].GetProperty("actor").GetString());
        Assert.AreEqual("removed", operations[0].GetProperty("outcome").GetString());
        Assert.IsFalse(operations.ToString().Contains("value-only-in-detail"));
    }

    [TestMethod]
    public async Task MissingCsrfAndInvalidBodiesCannotDeleteEntries()
    {
        await Cache.SetAsync("a", 1);
        using var noToken = await _http.PostAsJsonAsync("api/remove", new { keys = new[] { "a" } });
        Assert.AreEqual(HttpStatusCode.BadRequest, noToken.StatusCode);
        using var wrongTokenRequest = new HttpRequestMessage(HttpMethod.Post, "api/remove") { Content = JsonContent.Create(new { keys = new[] { "a" } }) };
        wrongTokenRequest.Headers.Add("RequestVerificationToken", "wrong");
        using var wrongToken = await _http.SendAsync(wrongTokenRequest);
        Assert.AreEqual(HttpStatusCode.BadRequest, wrongToken.StatusCode);
        foreach (var keys in new[] { Array.Empty<string>(), new[] { "a", "" }, Enumerable.Repeat("a", 201).ToArray() })
        {
            using var invalid = await Post("api/remove", new { keys });
            Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        using var invalidCursor = await _http.GetAsync("api/entries?cursor=invalid");
        Assert.AreEqual(HttpStatusCode.BadRequest, invalidCursor.StatusCode);
        using var repeated = await _http.GetAsync("api/entries?pageSize=1&pageSize=2");
        Assert.AreEqual(HttpStatusCode.BadRequest, repeated.StatusCode);
        Assert.IsTrue(await Cache.ExistsAsync("a"));
        using var valid = await Post("api/remove", new { keys = new[] { "a", "missing" } });
        Assert.AreEqual(HttpStatusCode.OK, valid.StatusCode);
        Assert.IsFalse(await Cache.ExistsAsync("a"));
    }

    [TestMethod]
    public async Task PrefixClearRequiresOwnedOneUsePreviewAndDeletesOnlySnapshot()
    {
        await Cache.SetAsync("product:1", 1);
        await Cache.SetAsync("product:2", 2);
        await Cache.SetAsync("other:1", 3);
        using var preview = await Post("api/preview", new { prefix = "product:" });
        preview.EnsureSuccessStatusCode();
        var plan = await preview.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(2, plan.GetProperty("keys").GetArrayLength());
        var token = plan.GetProperty("token").GetString();
        await Cache.SetAsync("product:new", 4);
        _http.DefaultRequestHeaders.Remove("X-Test-User");
        _http.DefaultRequestHeaders.Add("X-Test-User", "bob");
        using var otherUser = await Post("api/clear", new { token });
        Assert.AreEqual(HttpStatusCode.Conflict, otherUser.StatusCode);
        _http.DefaultRequestHeaders.Remove("X-Test-User");
        _http.DefaultRequestHeaders.Add("X-Test-User", "alice");
        using var clear = await Post("api/clear", new { token });
        clear.EnsureSuccessStatusCode();
        Assert.IsFalse(await Cache.ExistsAsync("product:1"));
        Assert.IsFalse(await Cache.ExistsAsync("product:2"));
        Assert.IsTrue(await Cache.ExistsAsync("product:new"));
        Assert.IsTrue(await Cache.ExistsAsync("other:1"));
        using var replay = await Post("api/clear", new { token });
        Assert.AreEqual(HttpStatusCode.Conflict, replay.StatusCode);
    }

    [TestMethod]
    public async Task PrefixPreviewIsBoundedAndReadOnlyBlocksEveryMutation()
    {
        for (var i = 0; i < 205; i++) await Cache.SetAsync("many:" + i.ToString("D3"), i);
        using var preview = await Post("api/preview", new { prefix = "many:" });
        preview.EnsureSuccessStatusCode();
        var plan = await preview.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(200, plan.GetProperty("keys").GetArrayLength());
        Assert.IsTrue(plan.GetProperty("partial").GetBoolean());
        _app.Services.GetRequiredService<SimpleCacheDashboardOptions>().ReadOnly = true;
        foreach (var path in new[] { "api/remove", "api/preview", "api/clear" })
        {
            using var response = await Post(path, new { keys = new[] { "many:000" }, prefix = "many:", token = plan.GetProperty("token").GetString() });
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }
        Assert.IsTrue(await Cache.ExistsAsync("many:000"));
        Assert.AreEqual(50, (await Get("api/entries")).GetProperty("items").GetArrayLength());
    }

    [TestMethod]
    public async Task PreviewExpiresWithoutDeletingOrMutatingData()
    {
        await Cache.SetAsync("expiry:1", 1);
        using var preview = await Post("api/preview", new { prefix = "expiry:" });
        preview.EnsureSuccessStatusCode();
        var plan = await preview.Content.ReadFromJsonAsync<JsonElement>();
        _clock.Advance(TimeSpan.FromSeconds(121));
        using var expired = await Post("api/clear", new { token = plan.GetProperty("token").GetString() });
        Assert.AreEqual(HttpStatusCode.Conflict, expired.StatusCode);
        Assert.IsTrue(await Cache.ExistsAsync("expiry:1"));
        Assert.AreEqual(0, (await Get("api/operations")).GetArrayLength());
    }

    [DataTestMethod]
    [DataRow(CacheDashboardScope.Prefixes), DataRow(CacheDashboardScope.All), DataRow(CacheDashboardScope.AllManaged)]
    public async Task ExpandedScopeUsesFullKeysAndValidatesEveryManagementEndpoint(CacheDashboardScope scope)
    {
        await Stop();
        await StartHost(o =>
        {
            o.Scope = scope;
            o.Prefixes = scope == CacheDashboardScope.Prefixes ? ["allowed:", "second:"] : [];
        });
        var store = _app.Services.GetRequiredService<ISimpleCacheStore>();
        foreach (var key in new[] { "allowed:1", "allowed:2", "second:1", "outside:1" })
            await store.WriteAsync(key, new("test", System.Text.Encoding.UTF8.GetBytes("42")), new(null, null), default);
        var status = await Get("api/status");
        Assert.AreEqual(scope.ToString(), status.GetProperty("scope").GetString());
        var items = (await Get("api/entries")).GetProperty("items");
        Assert.AreEqual(scope == CacheDashboardScope.Prefixes ? 3 : 4, items.GetArrayLength());
        Assert.IsTrue(items.EnumerateArray().Any(i => i.GetProperty("key").GetString() == "second:1"));
        var detail = await Get("api/entry?key=second:1");
        Assert.AreEqual("second:1", detail.GetProperty("entry").GetProperty("key").GetString());
        if (scope == CacheDashboardScope.Prefixes)
        {
            using var outsideDetail = await _http.GetAsync("api/entry?key=outside:1");
            Assert.AreEqual(HttpStatusCode.BadRequest, outsideDetail.StatusCode);
            // Validate the whole request before deleting even its first in-scope key.
            using var mixed = await Post("api/remove", new { keys = new[] { "allowed:1", "outside:1" } });
            Assert.AreEqual(HttpStatusCode.BadRequest, mixed.StatusCode);
            Assert.IsNotNull(await ((ISimpleCacheAdminStore)store).PeekAsync("allowed:1", default));
            Assert.AreEqual(0, (await Get("api/entries?prefix=outside:")).GetProperty("items").GetArrayLength());
        }
        else
        {
            using var emptyClear = await Post("api/preview", new { prefix = "" });
            Assert.AreEqual(HttpStatusCode.BadRequest, emptyClear.StatusCode);
            using var whitespaceClear = await Post("api/preview", new { prefix = " " });
            Assert.AreEqual(HttpStatusCode.BadRequest, whitespaceClear.StatusCode);
        }
        using var preview = await Post("api/preview", new { prefix = "allowed:" });
        preview.EnsureSuccessStatusCode();
        var plan = await preview.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(2, plan.GetProperty("keys").GetArrayLength());
        using var clear = await Post("api/clear", new { token = plan.GetProperty("token").GetString() });
        clear.EnsureSuccessStatusCode();
        Assert.IsNull(await ((ISimpleCacheAdminStore)store).PeekAsync("allowed:1", default));
        Assert.IsNotNull(await ((ISimpleCacheAdminStore)store).PeekAsync("outside:1", default));
        using var remove = await Post("api/remove", new { keys = new[] { "second:1" } });
        remove.EnsureSuccessStatusCode();
        var audit = await Get("api/operations");
        Assert.IsTrue(audit.EnumerateArray().Any(i => i.GetProperty("key").GetString() == "second:1"));
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        internal void Advance(TimeSpan duration) => _now += duration;
    }

    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var user = Request.Headers["X-Test-User"].ToString();
            if (string.IsNullOrEmpty(user)) return Task.FromResult(AuthenticateResult.NoResult());
            var role = Request.Headers["X-Test-Role"].ToString();
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user), new Claim(ClaimTypes.Name, user),
                new Claim(ClaimTypes.Role, string.IsNullOrEmpty(role) ? "admin" : role) }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
