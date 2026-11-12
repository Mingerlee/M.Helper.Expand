using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Logging.Test;

[TestClass]
public sealed class LoggingIntegrationTests
{
    private string _directory = null!;
    private string Database => Path.Combine(_directory, "logs.db");

    [TestInitialize]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "helper-logging-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, true);
    }

    [TestMethod]
    public async Task ConcurrentLogs_DrainOnStop_PreserveContextAndSurviveRestart()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Orders");
        using (var activity = new Activity("order").SetIdFormat(ActivityIdFormat.W3C).Start())
        using (logger.BeginScope(new Dictionary<string, object?> { ["UserId"] = 42 }))
        {
            logger.LogError(new EventId(7, "SaveFailed"), new InvalidOperationException("database unavailable"),
                "订单 {OrderId} 保存失败", 123);
            var traceId = activity.TraceId.ToString();
            await Parallel.ForEachAsync(Enumerable.Range(0, 300), async (i, _) =>
            {
                logger.LogInformation("并发日志 {Number}", i);
                await Task.Yield();
            });
            await host.StopAsync();
            var store = host.Services.GetRequiredService<SqliteLogStore>();
            var status = store.GetStatus();
            Assert.AreEqual(0L, status.Dropped);
            Assert.AreEqual(status.Accepted, status.Persisted);
            var page = await store.QueryAsync(new LogQuery { Level = 4, TraceId = traceId });
            var entry = page.Items.Single();
            Assert.AreEqual("订单 {OrderId} 保存失败", entry.MessageTemplate);
            Assert.AreEqual("订单 123 保存失败", entry.Message);
            Assert.AreEqual(123, JsonDocument.Parse(entry.PropertiesJson).RootElement.GetProperty("OrderId").GetInt32());
            StringAssert.Contains(entry.ScopesJson, "UserId");
            StringAssert.Contains(entry.Exception!, "database unavailable");
            Assert.AreEqual(7, entry.EventId);
            Assert.IsFalse(status.IsWriting);
        }
        using var restarted = BuildHost();
        await restarted.StartAsync();
        var recovered = await restarted.Services.GetRequiredService<SqliteLogStore>().QueryAsync(new LogQuery { Category = "Orders" });
        Assert.AreEqual(50, recovered.Items.Count);
        await restarted.StopAsync();
    }

    [TestMethod]
    public async Task Queries_UseStableCursor_LiteralKeyword_AndReadDuringWrites()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Search");
        for (var i = 0; i < 12; i++) logger.LogInformation("item {Number} literal %_' OR 1=1 --", i);
        await host.StopAsync();
        var store = host.Services.GetRequiredService<SqliteLogStore>();
        var first = await store.QueryAsync(new LogQuery { Category = "Search", PageSize = 5 });
        var second = await store.QueryAsync(new LogQuery { Category = "Search", PageSize = 5, BeforeId = first.NextBeforeId });
        Assert.AreEqual(5, first.Items.Count);
        Assert.AreEqual(5, second.Items.Count);
        Assert.IsFalse(first.Items.Select(x => x.Id).Intersect(second.Items.Select(x => x.Id)).Any());
        var literal = await store.QueryAsync(new LogQuery { Keyword = "%_' OR 1=1 --" });
        Assert.AreEqual(12, literal.Items.Count);
        var none = await store.QueryAsync(new LogQuery { Keyword = "' OR 1=1 --missing" });
        Assert.AreEqual(0, none.Items.Count);
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => store.QueryAsync(new LogQuery { PageSize = 201 }));

        using var running = BuildHost();
        await running.StartAsync();
        var writer = running.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Live");
        var reads = running.Services.GetRequiredService<SqliteLogStore>();
        for (var i = 0; i < 50; i++)
        {
            writer.LogWarning("实时消息 {Number}", i);
            await reads.QueryAsync(new LogQuery { PageSize = 5 });
        }
        await running.StopAsync();
        Assert.AreEqual(0L, reads.GetStatus().WriteFailures);
    }

    [TestMethod]
    public async Task Retention_RemovesExpiredLogs_StatisticsIncludeCritical_AndRoundTripDetails()
    {
        var options = Options();
        var buffer = new LogBuffer(options);
        var store = new SqliteLogStore(options, buffer);
        store.Initialize();
        store.Write([
            Entry(DateTimeOffset.UtcNow.AddDays(-20), 4),
            Entry(DateTimeOffset.UtcNow, 4),
            Entry(DateTimeOffset.UtcNow, 5),
            Entry(DateTimeOffset.UtcNow, 2)
        ]);
        Assert.AreEqual(1, store.DeleteExpiredBatch());
        var stats = await store.GetStatisticsAsync(480);
        Assert.AreEqual(3L, stats.Total);
        Assert.AreEqual(2L, stats.TodayErrors);
        Assert.AreEqual(24, stats.Trend.Count);
        Assert.AreEqual(3L, stats.Trend.Sum(x => x.Total));
        var errors = await store.QueryAsync(new LogQuery { MinimumLevel = 4 });
        Assert.AreEqual(2, errors.Items.Count);
        var detail = await store.GetAsync(errors.Items[0].Id);
        Assert.IsNotNull(detail);
        Assert.IsNull(await store.GetAsync(long.MaxValue));
    }

    [TestMethod]
    public void Overflow_AndBadFormatter_DoNotThrowOrBlockBusiness()
    {
        var options = Options();
        options.QueueCapacity = 1;
        options.BatchSize = 1;
        var buffer = new LogBuffer(options);
        using var provider = new SimpleLoggerProvider(options, buffer);
        var logger = provider.CreateLogger("Full");
        logger.LogInformation("first");
        logger.LogInformation("second");
        logger.Log<string>(LogLevel.Error, default, "bad", null, (_, _) => throw new Exception("formatter failed"));
        Assert.AreEqual(1L, buffer.Status().Accepted);
        Assert.AreEqual(1L, buffer.Status().Dropped);
        Assert.AreEqual(1L, buffer.Status().CaptureFailures);
    }

    [TestMethod]
    public async Task FailedWrite_RetriesBatch_AndExposesFailureWithoutRecursion()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var store = host.Services.GetRequiredService<SqliteLogStore>();
        // 触发真实 SQLite 写入错误，然后恢复表，确认原批次得到重试而非静默丢弃。
        using var connection = new SqliteConnection($"Data Source={Database}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TRIGGER deny_insert BEFORE INSERT ON helper_logs BEGIN SELECT RAISE(ABORT, 'test'); END;";
        command.ExecuteNonQuery();
        host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Retry").LogError("retry-me");
        await UntilAsync(() => store.GetStatus().WriteFailures > 0);
        Assert.AreEqual("SqliteException", store.GetStatus().LastError);
        command.CommandText = "DROP TRIGGER deny_insert";
        command.ExecuteNonQuery();
        await host.StopAsync();
        Assert.AreEqual(1, (await store.QueryAsync(new LogQuery { Category = "Retry" })).Items.Count);
        Assert.AreEqual(0L, store.GetStatus().Dropped);
    }

    [TestMethod]
    public async Task Dashboard_ProtectsPageAndEveryApi_ServesEmbeddedHtml_AndRejectsBadQueries()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddSimpleLogging(o => o.DatabasePath = Database);
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        await using var app = builder.Build();
        app.UsePathBase("/prefix");
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapSimpleLogDashboard("/nested/logs");
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address + "/prefix/") };
        foreach (var path in new[] { "", "/", "/api/logs", "/api/logs/1", "/api/status", "/api/statistics" })
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("nested/logs" + path)).StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-Auth", "yes");
        var page = await client.GetAsync("nested/logs");
        Assert.AreEqual(HttpStatusCode.OK, page.StatusCode);
        StringAssert.Contains(await page.Content.ReadAsStringAsync(), "日志工作台");
        Assert.IsTrue(page.Headers.CacheControl!.NoStore);
        Assert.IsTrue(page.Headers.Contains("Content-Security-Policy"));
        foreach (var query in new[] { "pageSize=201", "level=no", "from=bad", "beforeId=-1", "level=2&level=3", "minimumLevel=7" })
            Assert.AreEqual(HttpStatusCode.BadRequest, (await client.GetAsync("nested/logs/api/logs?" + query)).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.GetAsync("nested/logs/api/statistics?utcOffsetMinutes=9999")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("nested/logs/api/logs/999999")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("nested/logs/api/statistics")).StatusCode);
        Assert.IsNotNull(await client.GetFromJsonAsync<LoggingStatus>("nested/logs/api/status"));
        await app.StopAsync();
    }

    [TestMethod]
    public async Task PersistentWriteFailure_ShutdownRemainsBounded_AndCountsUnwrittenLogs()
    {
        using var host = BuildHost(o => o.ShutdownTimeout = TimeSpan.FromMilliseconds(100));
        await host.StartAsync();
        var store = host.Services.GetRequiredService<SqliteLogStore>();
        using var connection = new SqliteConnection($"Data Source={Database}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TRIGGER deny_insert BEFORE INSERT ON helper_logs BEGIN SELECT RAISE(ABORT, 'test'); END;";
        command.ExecuteNonQuery();
        host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("FailStop").LogError("cannot-write");
        await UntilAsync(() => store.GetStatus().WriteFailures > 0);
        var watch = Stopwatch.StartNew();
        await host.StopAsync();
        Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(2));
        await UntilAsync(() => !store.GetStatus().IsWriting);
        var status = store.GetStatus();
        Assert.IsTrue(status.Dropped >= 1);
        Assert.AreEqual(status.Accepted - status.Persisted, status.Dropped);
    }

    [TestMethod]
    public async Task OversizedFields_KeepValidJson_AndEnrichmentCanRedact()
    {
        using var host = BuildHost(o =>
        {
            o.MaxFieldLength = 256;
            o.Enrich = entry => entry.Message = entry.Message.Replace("secret", "***", StringComparison.Ordinal);
        });
        await host.StartAsync();
        host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Bounded").LogInformation("secret {Payload}", new string('x', 5000));
        await host.StopAsync();
        var entry = (await host.Services.GetRequiredService<SqliteLogStore>().QueryAsync(new LogQuery { Category = "Bounded" })).Items.Single();
        StringAssert.Contains(entry.Message, "***");
        Assert.IsFalse(entry.Message.Contains("secret"));
        Assert.IsTrue(entry.Message.Length < 300);
        Assert.IsTrue(JsonDocument.Parse(entry.PropertiesJson).RootElement.GetProperty("_truncated").GetBoolean());
    }

    private IHost BuildHost(Action<SimpleLoggingOptions>? configure = null) => new HostBuilder()
        .ConfigureLogging(logging => logging.ClearProviders())
        .ConfigureServices(services => services.AddSimpleLogging(o =>
        {
            o.DatabasePath = Database;
            o.FlushInterval = TimeSpan.FromMilliseconds(20);
            configure?.Invoke(o);
        })).Build();

    private SimpleLoggingOptions Options() => new() { DatabasePath = Database };
    private static LogEntry Entry(DateTimeOffset time, int level) => new()
    {
        TimestampUtc = time,
        Level = level,
        Message = "test",
        Category = "Tests"
    };

    private static async Task UntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while (!condition()) await Task.Delay(20, timeout.Token);
    }
}

internal sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(
        Request.Headers.ContainsKey("X-Test-Auth")
            ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "tester")], "Test")), "Test"))
            : AuthenticateResult.NoResult());
}
