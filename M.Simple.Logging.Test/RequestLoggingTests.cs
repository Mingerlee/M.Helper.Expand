using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Logging.Test;

[TestClass]
public sealed class RequestLoggingTests
{
    private string _directory = null!;
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private SqliteLogStore Store => _app.Services.GetRequiredService<SqliteLogStore>();

    [TestInitialize]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "simple-request-logging", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        _client?.Dispose();
        if (_app is not null) await _app.DisposeAsync();
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, true);
    }

    [TestMethod]
    public async Task Json_Query_Routes_AreRedacted_AndBodyIsStillReadable()
    {
        await Start(o => o.SensitiveFields.Add("IdentityNumber"));
        const string body = """{"OrderId":42,"Password":"hidden-password","nested":{"access_token":"hidden-token"},"items":[{"IdentityNumber":"hidden-id"}]}""";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/json/42?Name=alice&API_Key=hidden-key&x=1&x=2")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", "Bearer hidden-header");
        request.Headers.Add("Cookie", "session=hidden-cookie");
        var response = await _client.SendAsync(request);
        Assert.AreEqual(body, await response.Content.ReadAsStringAsync());
        await _app.StopAsync();
        var entries = (await Store.QueryAsync(new LogQuery { Category = "Test.Requests" })).Items;
        Assert.AreEqual(4, entries.Count);
        foreach (var entry in entries.Where(e => e.Level >= 4))
        {
            using var document = JsonDocument.Parse(entry.PropertiesJson);
            var context = document.RootElement.GetProperty("Request");
            Assert.AreEqual("POST", context.GetProperty("Method").GetString());
            Assert.AreEqual("/json/42", context.GetProperty("Path").GetString());
            Assert.AreEqual("42", context.GetProperty("RouteValues").GetProperty("orderId").GetString());
            Assert.AreEqual("alice", context.GetProperty("Query").GetProperty("Name")[0].GetString());
            Assert.AreEqual(2, context.GetProperty("Query").GetProperty("x").GetArrayLength());
            Assert.AreEqual("[REDACTED]", context.GetProperty("Body").GetProperty("Password").GetString());
            Assert.AreEqual(42, context.GetProperty("Body").GetProperty("OrderId").GetInt32());
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.TraceId));
            foreach (var secret in new[] { "hidden-password", "hidden-token", "hidden-id", "hidden-key", "hidden-header", "hidden-cookie" })
                Assert.IsFalse(entry.PropertiesJson.Contains(secret), "请求快照不应包含敏感信息。");
        }
        foreach (var entry in entries.Where(e => e.Level < 4))
            Assert.IsFalse(JsonDocument.Parse(entry.PropertiesJson).RootElement.TryGetProperty("Request", out _));
    }

    [TestMethod]
    public async Task UrlEncodedForm_AndMultipartMetadata_PreserveBindingAndExcludeFileContents()
    {
        await Start();
        var form = await _client.PostAsync("/form", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["OrderId"] = "17", ["password"] = "form-secret"
        }));
        Assert.AreEqual("17", await form.Content.ReadAsStringAsync());
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent("19"), "OrderId");
        multipart.Add(new StringContent("multipart-secret"), "password");
        multipart.Add(new StringContent("file-bytes-must-not-be-logged"), "document", "sample.txt");
        var uploaded = await _client.PostAsync("/upload", multipart);
        Assert.AreEqual("19:sample.txt", await uploaded.Content.ReadAsStringAsync());
        await _app.StopAsync();
        var entries = (await Store.QueryAsync(new LogQuery { Category = "Test.Requests" })).Items;
        Assert.AreEqual(2, entries.Count);
        var upload = entries.Single(e => e.Message == "upload failed");
        using var document = JsonDocument.Parse(upload.PropertiesJson);
        var context = document.RootElement.GetProperty("Request");
        Assert.AreEqual("CapturedFormMetadata", context.GetProperty("BodyStatus").GetString());
        Assert.AreEqual("sample.txt", context.GetProperty("Body").GetProperty("Files")[0].GetProperty("FileName").GetString());
        foreach (var entry in entries)
            foreach (var secret in new[] { "form-secret", "multipart-secret", "file-bytes-must-not-be-logged" })
                Assert.IsFalse(entry.PropertiesJson.Contains(secret));
    }

    [TestMethod]
    public async Task Oversized_Invalid_Unsupported_AndUnknownLengthBodies_AreNotStoredRaw()
    {
        await Start(o => o.MaxBodyBytes = 1024);
        var large = "{\"Password\":\"large-secret\",\"data\":\"" + new string('x', 2000) + "\"}";
        foreach (var content in new HttpContent[]
        {
            new StringContent(large, Encoding.UTF8, "application/json"),
            new UnknownLengthContent(large),
            new StringContent("{\"Password\":\"invalid-secret\"", Encoding.UTF8, "application/json"),
            new StringContent("plain-secret", Encoding.UTF8, "text/plain")
        })
        {
            using (content)
            {
                if (content is UnknownLengthContent)
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var expected = content is UnknownLengthContent ? large : await content.ReadAsStringAsync();
                var response = await _client.PostAsync("/json/17", content);
                Assert.AreEqual(expected, await response.Content.ReadAsStringAsync());
            }
        }
        await _app.StopAsync();
        var entries = (await Store.QueryAsync(new LogQuery { Category = "Test.Requests", Level = 4 })).Items;
        Assert.AreEqual(4, entries.Count);
        foreach (var entry in entries)
        {
            using var document = JsonDocument.Parse(entry.PropertiesJson);
            var context = document.RootElement.GetProperty("Request");
            Assert.AreEqual(JsonValueKind.Null, context.GetProperty("Body").ValueKind);
            Assert.IsFalse(entry.PropertiesJson.Contains("secret"));
        }
        CollectionAssert.AreEquivalent(new[] { "TooLarge", "TooLarge", "UnavailableOrInvalid", "UnsupportedContentType" },
            entries.Select(e => JsonDocument.Parse(e.PropertiesJson).RootElement.GetProperty("Request").GetProperty("BodyStatus").GetString()).ToArray());
    }

    [TestMethod]
    public async Task UnhandledException_LogsAndPropagatesToHostHandler_WithoutCapturingBackgroundLogs()
    {
        await Start();
        var response = await _client.PostAsync("/throw", new StringContent("{\"OrderId\":71}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.AreEqual("handled-by-host", await response.Content.ReadAsStringAsync());
        _app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Outside").LogError("outside request");
        await _app.StopAsync();
        var entries = (await Store.QueryAsync(new LogQuery { Level = 4 })).Items;
        var unhandled = entries.Single(e => e.EventId == 1001);
        StringAssert.Contains(unhandled.Exception!, "test-unhandled");
        using var document = JsonDocument.Parse(unhandled.PropertiesJson);
        Assert.AreEqual(71, document.RootElement.GetProperty("Request").GetProperty("Body").GetProperty("OrderId").GetInt32());
        var outside = entries.Single(e => e.Category == "Outside");
        Assert.IsFalse(JsonDocument.Parse(outside.PropertiesJson).RootElement.TryGetProperty("Request", out _));
    }

    [TestMethod]
    public async Task ParallelRequests_KeepTheirOwnParameters()
    {
        await Start();
        await Parallel.ForEachAsync(Enumerable.Range(1, 25), async (id, _) =>
        {
            var body = "{\"OrderId\":" + id + "}";
            var response = await _client.PostAsync("/json/" + id, new StringContent(body, Encoding.UTF8, "application/json"));
            Assert.AreEqual(body, await response.Content.ReadAsStringAsync());
        });
        await _app.StopAsync();
        var entries = (await Store.QueryAsync(new LogQuery { Category = "Test.Requests", Level = 4 })).Items;
        Assert.AreEqual(25, entries.Count);
        foreach (var entry in entries)
        {
            using var document = JsonDocument.Parse(entry.PropertiesJson);
            var context = document.RootElement.GetProperty("Request");
            Assert.AreEqual(context.GetProperty("RouteValues").GetProperty("orderId").GetString(),
                context.GetProperty("Body").GetProperty("OrderId").GetInt32().ToString());
        }
    }

    [TestMethod]
    public async Task DisabledByDefault_AndPathFilterSkipRequestCapture()
    {
        await Start(installMiddleware: false);
        await _client.PostAsync("/json/1", new StringContent("{\"OrderId\":1}", Encoding.UTF8, "application/json"));
        await _app.StopAsync();
        foreach (var entry in (await Store.QueryAsync(new LogQuery { Category = "Test.Requests" })).Items)
            Assert.IsFalse(JsonDocument.Parse(entry.PropertiesJson).RootElement.TryGetProperty("Request", out _));
    }

    [TestMethod]
    public async Task JsonModelBinding_AndContextBudget_ArePreserved()
    {
        await Start(o => o.MaxBodyBytes = 1024);
        var response = await _client.PostAsync("/bound", new StringContent("{\"OrderId\":81}", Encoding.UTF8, "application/json"));
        Assert.AreEqual("81", await response.Content.ReadAsStringAsync());
        var largeQuery = string.Join('&', Enumerable.Range(0, 40).Select(i => "field" + i + "=" + new string('a', 80)));
        await _client.PostAsync("/json/1?" + largeQuery, new StringContent("{\"OrderId\":1}", Encoding.UTF8, "application/json"));
        await _app.StopAsync();
        var entries = (await Store.QueryAsync(new LogQuery { Category = "Test.Requests", Level = 4 })).Items;
        var bound = entries.Single(e => e.Message == "binding failed");
        using var document = JsonDocument.Parse(bound.PropertiesJson);
        Assert.AreEqual(81, document.RootElement.GetProperty("Request").GetProperty("Body").GetProperty("OrderId").GetInt32());
        var large = entries.Single(e => e.Message == "request failed");
        using var largeDocument = JsonDocument.Parse(large.PropertiesJson);
        Assert.AreEqual("ContextTooLarge", largeDocument.RootElement.GetProperty("Request").GetProperty("BodyStatus").GetString());
        Assert.IsTrue(Encoding.UTF8.GetByteCount(large.PropertiesJson) < 1200);
    }

    [TestMethod]
    public async Task BodyCanBeDisabled_AndRoutesCanBeExcluded()
    {
        await Start(o =>
        {
            o.IncludeBody = false;
            o.ShouldCapture = http => !http.Request.Path.StartsWithSegments("/json/2");
        });
        await _client.PostAsync("/json/1", new StringContent("{\"OrderId\":1}", Encoding.UTF8, "application/json"));
        await _client.PostAsync("/json/2", new StringContent("{\"OrderId\":2}", Encoding.UTF8, "application/json"));
        await _app.StopAsync();
        var entries = (await Store.QueryAsync(new LogQuery { Category = "Test.Requests", Level = 4 })).Items;
        var captured = entries.Select(e => JsonDocument.Parse(e.PropertiesJson)).Where(d => d.RootElement.TryGetProperty("Request", out _)).ToArray();
        Assert.AreEqual(1, captured.Length);
        Assert.AreEqual("Disabled", captured[0].RootElement.GetProperty("Request").GetProperty("BodyStatus").GetString());
    }

    [TestMethod]
    public async Task UnhandledLoggingCanBeDisabled_AndPlain500DoesNotGenerateLogs()
    {
        await Start(o => o.LogUnhandledExceptions = false);
        var thrown = await _client.PostAsync("/throw", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.InternalServerError, thrown.StatusCode);
        var status = await _client.GetAsync("/status500");
        Assert.AreEqual(HttpStatusCode.InternalServerError, status.StatusCode);
        await _app.StopAsync();
        Assert.AreEqual(0, (await Store.QueryAsync(new LogQuery { Level = 4 })).Items.Count);
    }

    [TestMethod]
    public async Task PerLogSwitch_OverridesEnabledDefault_WithoutAffectingNextLog()
    {
        await VerifyPerLogSwitch(defaultInclude: true);
    }

    [TestMethod]
    public async Task PerLogSwitch_OverridesDisabledDefault_AndPreservesTemplateArgumentsAndException()
    {
        await VerifyPerLogSwitch(defaultInclude: false);
    }

    [TestMethod]
    public async Task PerLogEnable_WithoutMiddleware_DoesNotInventRequestContext()
    {
        await Start(installMiddleware: false);
        await _client.PostAsync("/switch", new StringContent("{\"OrderId\":99}", Encoding.UTF8, "application/json"));
        await _app.StopAsync();
        var entries = (await Store.QueryAsync(new LogQuery { Category = "Test.Switch" })).Items;
        Assert.AreEqual(6, entries.Count);
        foreach (var entry in entries)
            Assert.IsFalse(JsonDocument.Parse(entry.PropertiesJson).RootElement.TryGetProperty("Request", out _));
    }

    private async Task VerifyPerLogSwitch(bool defaultInclude)
    {
        await Start(o => o.IncludeInErrorLogs = defaultInclude);
        await _client.PostAsync("/switch", new StringContent("{\"OrderId\":99}", Encoding.UTF8, "application/json"));
        await _app.StopAsync();
        var entries = (await Store.QueryAsync(new LogQuery { Category = "Test.Switch" })).Items;
        Assert.AreEqual(6, entries.Count);
        foreach (var entry in entries)
        {
            using var document = JsonDocument.Parse(entry.PropertiesJson);
            var root = document.RootElement;
            var expected = entry.Message.StartsWith("enabled", StringComparison.Ordinal) ||
                entry.Message.StartsWith("default", StringComparison.Ordinal) && defaultInclude;
            Assert.AreEqual(expected, root.TryGetProperty("Request", out var request));
            Assert.AreEqual(99, root.GetProperty("OrderId").GetInt32());
            Assert.IsFalse(root.TryGetProperty("IncludeRequestContext", out _));
            if (expected) Assert.AreEqual(99, request.GetProperty("Body").GetProperty("OrderId").GetInt32());
        }
        var eventEntry = entries.Single(e => e.EventId == 77);
        Assert.AreEqual("SaveFailed", eventEntry.EventName);
        Assert.AreEqual("enabled event-ex {OrderId}", eventEntry.MessageTemplate);
        StringAssert.Contains(eventEntry.Exception!, "controlled-exception");
        Assert.AreEqual(1, entries.Count(e => e.EventId == 78));
        Assert.AreEqual(2, entries.Count(e => e.Exception is not null));
    }

    [TestMethod]
    public async Task RequestScope_IsClearedInDetachedTasks_AndAbortIsNotLoggedAsUnhandled()
    {
        var http = new DefaultHttpContext();
        http.Request.ContentType = "text/plain";
        Task<RequestLogContext?> detached;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (RequestLogContext.Push(new RequestLogContext(http, new SimpleRequestLoggingOptions())))
        {
            detached = Task.Run(async () => { await release.Task; return RequestLogContext.Current; });
            Assert.IsNotNull(RequestLogContext.Current);
        }
        release.SetResult();
        Assert.IsNull(await detached);
        Assert.IsNull(RequestLogContext.Current);
        var options = new SimpleLoggingOptions();
        var buffer = new LogBuffer(options);
        using var provider = new SimpleLoggerProvider(options, buffer);
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        http.RequestAborted = new CancellationToken(true);
        var middleware = new SimpleRequestLoggingMiddleware(_ => throw new OperationCanceledException(http.RequestAborted),
            new SimpleRequestLoggingOptions(), factory.CreateLogger<SimpleRequestLoggingMiddleware>());
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => middleware.InvokeAsync(http));
        Assert.AreEqual(0L, buffer.Status().Accepted);
        Assert.IsNull(RequestLogContext.Current);
    }

    private async Task Start(Action<SimpleRequestLoggingOptions>? configure = null, bool installMiddleware = true)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddSimpleLogging(o =>
        {
            o.DatabasePath = Path.Combine(_directory, "logs.db");
            o.FlushInterval = TimeSpan.FromMilliseconds(10);
        });
        _app = builder.Build();
        // 模拟宿主异常处理层，不改变已有错误响应逻辑。
        _app.Use(async (context, next) =>
        {
            try { await next(context); }
            catch (InvalidOperationException)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("handled-by-host");
            }
        });
        if (installMiddleware) _app.UseSimpleLoggingRequestContext(configure);
        _app.MapPost("/json/{orderId}", async (HttpContext http, ILoggerFactory loggers) =>
        {
            using var reader = new StreamReader(http.Request.Body);
            var text = await reader.ReadToEndAsync();
            await Task.Delay(5);
            var logger = loggers.CreateLogger("Test.Requests");
            logger.LogInformation("normal");
            logger.LogWarning("warning");
            logger.LogError("request failed");
            logger.LogCritical("critical failure");
            return Results.Text(text);
        });
        _app.MapPost("/form", async (HttpContext http, ILoggerFactory loggers) =>
        {
            var form = await http.Request.ReadFormAsync();
            loggers.CreateLogger("Test.Requests").LogError("form failed");
            return Results.Text(form["OrderId"].ToString());
        });
        _app.MapPost("/bound", (BoundOrder order, ILoggerFactory loggers) =>
        {
            loggers.CreateLogger("Test.Requests").LogError("binding failed");
            return Results.Text(order.OrderId.ToString());
        });
        _app.MapPost("/upload", async (HttpContext http, ILoggerFactory loggers) =>
        {
            var form = await http.Request.ReadFormAsync();
            loggers.CreateLogger("Test.Requests").LogError("upload failed");
            return Results.Text(form["OrderId"] + ":" + form.Files[0].FileName);
        });
        _app.MapPost("/throw", (RequestDelegate)(_ => throw new InvalidOperationException("test-unhandled")));
        _app.MapPost("/switch", (ILoggerFactory loggers) =>
        {
            var logger = loggers.CreateLogger("Test.Switch");
            var exception = new InvalidOperationException("controlled-exception");
            logger.LogError(true, "enabled plain {OrderId}", 99);
            logger.LogError(false, "disabled plain {OrderId}", 99);
            logger.LogError("default after-disabled {OrderId}", 99);
            logger.LogError(true, exception, "enabled exception {OrderId}", 99);
            logger.LogError(true, new EventId(78, "EventOnly"), "enabled event {OrderId}", 99);
            logger.LogError(true, new EventId(77, "SaveFailed"), exception, "enabled event-ex {OrderId}", 99);
            return Results.Ok();
        });
        _app.MapGet("/status500", () => Results.StatusCode(500));
        await _app.StartAsync();
        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    private sealed class UnknownLengthContent(string value) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(Encoding.UTF8.GetBytes(value)).AsTask();
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken ct) =>
            stream.Write(Encoding.UTF8.GetBytes(value));

    }

    public sealed record BoundOrder(int OrderId);
}
