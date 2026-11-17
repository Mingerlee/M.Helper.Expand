using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using M.Simple.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.AspNetCore.Response.Test;

[TestClass]
public sealed class CombinedExceptionLoggingTests
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "simple-combined-logging", Guid.NewGuid().ToString("N"));
    private readonly RecordingProvider _records = new();
    private readonly Exception _sharedException = new InvalidOperationException("combined-test-exception");

    [TestCleanup]
    public async Task Cleanup()
    {
        _client?.Dispose();
        if (_app is not null) await _app.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    private async Task Start(bool loggingFirst = false, bool logUnhandled = true, bool capture = true,
        bool installRequestLogging = true, bool disableRequestLogger = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders().AddProvider(_records);
        if (disableRequestLogger)
            builder.Logging.AddFilter("M.Simple.Logging.SimpleRequestLoggingMiddleware", LogLevel.None);
        builder.Services.AddUnifiedResponse();
        builder.Services.AddSimpleLogging(options =>
        {
            options.DatabasePath = Path.Combine(_directory, "logs.db");
            options.FlushInterval = TimeSpan.FromMilliseconds(10);
            options.MinimumLevel = LogLevel.Error;
        });
        _app = builder.Build();
        void AddRequestLogging() => _app.UseSimpleLoggingRequestContext(options =>
        {
            options.LogUnhandledExceptions = logUnhandled;
            options.ShouldCapture = _ => capture;
        });
        if (loggingFirst && installRequestLogging) AddRequestLogging();
        _app.UseUnifiedResponse();
        if (!loggingFirst && installRequestLogging) AddRequestLogging();
        _app.MapPost("/failure/{id}", (RequestDelegate)(context =>
        {
            if (context.Request.Query.ContainsKey("business"))
                context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Test.Business")
                    .LogError(new EventId(2002, "BusinessError"), "separate business failure");
            throw _sharedException;
        }));
        _app.MapPost("/skipped", (RequestDelegate)(_ => throw _sharedException))
            .WithMetadata(new SkipUnifiedResponseAttribute());
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    private async Task<HttpResponseMessage> Fail(string path = "/failure/42?name=alice&token=query-secret")
        => await _client.PostAsync(path, new StringContent("""{"OrderId":42,"Password":"body-secret"}""", Encoding.UTF8, "application/json"));

    private async Task<IReadOnlyList<LogEntry>> Logs()
    {
        await _app.StopAsync(); // Flush the real SQLite writer before asserting the stored rows.
        return (await _app.Services.GetRequiredService<SqliteLogStore>().QueryAsync(new LogQuery { MinimumLevel = 4 })).Items;
    }

    private static void AssertRequest(LogEntry entry)
    {
        using var doc = JsonDocument.Parse(entry.PropertiesJson);
        var request = doc.RootElement.GetProperty("Request");
        Assert.AreEqual("POST", request.GetProperty("Method").GetString());
        Assert.AreEqual("alice", request.GetProperty("Query").GetProperty("name")[0].GetString());
        Assert.AreEqual("42", request.GetProperty("RouteValues").GetProperty("id").GetString());
        Assert.AreEqual(42, request.GetProperty("Body").GetProperty("OrderId").GetInt32());
        Assert.AreEqual("[REDACTED]", request.GetProperty("Body").GetProperty("Password").GetString());
        Assert.IsFalse(entry.PropertiesJson.Contains("body-secret"));
        Assert.IsFalse(entry.PropertiesJson.Contains("query-secret"));
        Assert.IsTrue(entry.Exception!.Contains("combined-test-exception"));
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task BothMiddlewareOrdersProduceOneStoredErrorWithParameters(bool loggingFirst)
    {
        await Start(loggingFirst: loggingFirst);
        var response = await Fail();
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(500, json.GetProperty("code").GetInt32());
        Assert.AreEqual("服务器内部错误", json.GetProperty("message").GetString());
        var entries = await Logs();
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(1, _records.Errors.Count, "All providers should receive one automatic error, including framework categories.");
        AssertRequest(entries.Single());
    }

    [DataTestMethod]
    [DataRow(false, true, false)]
    [DataRow(true, false, false)]
    [DataRow(true, true, true)]
    public async Task DisabledOrExcludedRequestLoggingFallsBackToOneResponseError(bool logUnhandled, bool capture, bool disableRequestLogger)
    {
        await Start(logUnhandled: logUnhandled, capture: capture, disableRequestLogger: disableRequestLogger);
        Assert.AreEqual(HttpStatusCode.InternalServerError, (await Fail()).StatusCode);
        var entries = await Logs();
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(1, _records.Errors.Count);
        Assert.IsTrue(entries.Single().Exception!.Contains("combined-test-exception"));
    }

    [TestMethod]
    public async Task ResponsePackageAloneStillLogsOneException()
    {
        await Start(installRequestLogging: false);
        Assert.AreEqual(HttpStatusCode.InternalServerError, (await Fail()).StatusCode);
        Assert.AreEqual(1, (await Logs()).Count);
        Assert.AreEqual(1, _records.Errors.Count);
    }

    [TestMethod]
    public async Task IdenticalExceptionInstanceInDifferentRequestsIsNotGloballyDeduplicated()
    {
        await Start();
        var responses = await Task.WhenAll(Fail(), Fail());
        Assert.IsTrue(responses.All(response => response.StatusCode == HttpStatusCode.InternalServerError));
        var entries = await Logs();
        Assert.AreEqual(2, entries.Count);
        Assert.AreEqual(2, _records.Errors.Count);
        foreach (var entry in entries) AssertRequest(entry);
    }

    [TestMethod]
    public async Task ExplicitBusinessErrorsArePreserved()
    {
        await Start();
        await Fail("/failure/42?business=true");
        var entries = await Logs();
        Assert.AreEqual(2, entries.Count);
        Assert.AreEqual(1, entries.Count(entry => entry.EventId == 2002));
        Assert.AreEqual(1, entries.Count(entry => entry.Exception is not null));
        Assert.AreEqual(2, _records.Errors.Count);
    }

    [TestMethod]
    public async Task SkippedResponseStillLogsOnlyOnce()
    {
        await Start();
        var response = await Fail("/skipped");
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync());
        var entries = await Logs();
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(1001, entries.Single().EventId);
        Assert.AreEqual(1, _records.Errors.Count);
    }

    private sealed class RecordingProvider : ILoggerProvider
    {
        public ConcurrentQueue<(string Category, Exception? Exception)> Errors { get; } = new();
        public ILogger CreateLogger(string categoryName) => new Recorder(this, categoryName);
        public void Dispose() { }
        private sealed class Recorder(RecordingProvider owner, string category) : ILogger
        {
            public bool IsEnabled(LogLevel level) => level >= LogLevel.Error;
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (IsEnabled(logLevel)) owner.Errors.Enqueue((category, exception));
            }
        }
    }
}
