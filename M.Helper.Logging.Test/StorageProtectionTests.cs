using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Helper.Logging.Test;

[TestClass]
public sealed class StorageProtectionTests
{
    private const long MiB = 1024 * 1024;
    private string _directory = null!;
    private string Database => Path.Combine(_directory, "logs.db");

    [TestInitialize]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "helper-storage-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, true);
    }

    [TestMethod]
    public async Task LowDisk_DropsLogs_UsesRecoveryMargin_AndAutomaticallyResumes()
    {
        var disk = new FakeDisk();
        using var host = Host(disk);
        await host.StartAsync();
        var store = host.Services.GetRequiredService<SqliteLogStore>();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DiskTest");
        logger.LogInformation("before-pause");
        await Until(() => store.GetStatus().Persisted >= 1);
        disk.Free = MiB - 1;
        await Until(() => store.GetStatus().Storage?.PauseReason == "LowDiskSpace");
        var dropped = store.GetStatus().Dropped;
        logger.LogError("must-not-persist");
        Assert.IsTrue(store.GetStatus().Dropped > dropped);
        Assert.AreEqual(0, (await store.QueryAsync(new LogQuery { Keyword = "must-not-persist" })).Items.Count);
        disk.Free = MiB + MiB / 2;
        await Task.Delay(120);
        Assert.IsTrue(store.GetStatus().Storage!.IsPaused);
        disk.Free = 2 * MiB;
        await Until(() => store.GetStatus().Storage?.IsPaused == false);
        logger.LogInformation("after-resume");
        await host.StopAsync();
        Assert.AreEqual(1, (await store.QueryAsync(new LogQuery { Keyword = "after-resume" })).Items.Count);
    }

    [TestMethod]
    public async Task LowDiskAtStartup_DoesNotCreateDatabase_AndRecoversWithoutRestart()
    {
        var disk = new FakeDisk { Free = 0 };
        using var host = Host(disk);
        await host.StartAsync();
        var store = host.Services.GetRequiredService<SqliteLogStore>();
        Assert.IsTrue(store.GetStatus().Storage!.IsPaused);
        Assert.IsFalse(File.Exists(Database));
        await Assert.ThrowsExceptionAsync<SqliteException>(() => store.QueryAsync(new LogQuery()));
        Assert.IsFalse(File.Exists(Database), "页面查询不得在低磁盘暂停时创建数据库。");
        disk.Free = 1024 * MiB;
        await Until(() => store.GetStatus().Storage?.IsPaused == false);
        host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup").LogWarning("recovered");
        await host.StopAsync();
        Assert.AreEqual(1, (await store.QueryAsync(new LogQuery { Category = "Startup" })).Items.Count);
    }

    [TestMethod]
    public async Task UnknownDiskSpace_FailsClosed_AndDoesNotThrowIntoBusiness()
    {
        var disk = new FakeDisk();
        using var host = Host(disk);
        await host.StartAsync();
        var store = host.Services.GetRequiredService<SqliteLogStore>();
        disk.Free = -1;
        await Until(() => store.GetStatus().Storage?.PauseReason == "DiskSpaceUnavailable");
        Assert.IsNull(store.GetStatus().Storage!.AvailableDiskBytes);
        host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Unknown").LogError("dropped");
        Assert.IsTrue(store.GetStatus().Dropped > 0);
        disk.Free = 1024 * MiB;
        await Until(() => store.GetStatus().Storage?.IsPaused == false);
        await host.StopAsync();
    }

    [TestMethod]
    public async Task Capacity_PrunesOldestLogs_ReclaimsPhysicalPages_AndPreservesRecentLogs()
    {
        var (options, store, buffer, guard) = Create();
        guard.Initialize();
        FillTo(store, 11 * MiB);
        Assert.IsTrue(store.Checkpoint());
        var original = store.FileSizes().Database;
        var newest = (await store.QueryAsync(new LogQuery { PageSize = 1 })).Items.Single().Id;
        Assert.IsTrue(guard.CanWrite([], CancellationToken.None));
        var status = buffer.Status().Storage!;
        Assert.IsTrue(status.PrunedLogs > 0);
        Assert.IsTrue(status.TotalBytes < options.MaxStorageBytes);
        Assert.IsTrue(store.FileSizes().Database < original, "增量回收应实际缩小新库文件。");
        Assert.IsNotNull(await store.GetAsync(newest));
        Assert.IsNull(await store.GetAsync(1));
        store.Write([Entry("still-writable")]);
        Assert.AreEqual(1, (await store.QueryAsync(new LogQuery { Keyword = "still-writable" })).Items.Count);
    }

    [TestMethod]
    public async Task PinnedWal_PausesWithoutDeletingHistory_ThenResumesAfterReaderReleases()
    {
        var (_, store, buffer, guard) = Create();
        guard.Initialize();
        store.Write([Entry("baseline")]);
        Assert.IsTrue(store.Checkpoint());
        using var connection = new SqliteConnection($"Data Source={Database}");
        connection.Open();
        using var transaction = connection.BeginTransaction(deferred: true);
        using (var read = connection.CreateCommand())
        {
            read.Transaction = transaction;
            read.CommandText = "SELECT COUNT(*) FROM helper_logs";
            read.ExecuteScalar();
        }
        FillTo(store, 11 * MiB);
        var files = store.FileSizes();
        Assert.IsTrue(files.Wal > 0);
        Assert.IsFalse(guard.CanWrite([], CancellationToken.None));
        var paused = buffer.Status().Storage!;
        Assert.AreEqual("CheckpointBusy", paused.PauseReason);
        Assert.AreEqual(0L, paused.PrunedLogs);
        Assert.AreEqual(files.Wal, paused.WalBytes);
        transaction.Rollback();
        await Task.Delay(80);
        Assert.IsTrue(guard.CanWrite([], CancellationToken.None));
        Assert.IsFalse(buffer.Status().Storage!.IsPaused);
        Assert.AreEqual(0L, store.FileSizes().Wal);
    }

    [TestMethod]
    public async Task LegacyDatabase_DoesNotVacuumOrRepeatedlyDeleteWhenPhysicalFileCannotShrink()
    {
        using (var old = new SqliteConnection($"Data Source={Database}"))
        {
            old.Open();
            using var command = old.CreateCommand();
            command.CommandText = "PRAGMA auto_vacuum=NONE; CREATE TABLE legacy_marker(id INTEGER);";
            command.ExecuteNonQuery();
        }
        var (options, store, buffer, guard) = Create();
        options.MaxStorageBytes = 32 * MiB;
        store.Initialize();
        FillTo(store, 18 * MiB);
        store.Checkpoint();
        options.MaxStorageBytes = 16 * MiB;
        guard.Initialize();
        Assert.IsFalse(guard.CanWrite([], CancellationToken.None));
        Assert.AreEqual("StorageLimit", buffer.Status().Storage!.PauseReason);
        var pruned = buffer.Status().Storage!.PrunedLogs;
        await Task.Delay(80);
        Assert.IsFalse(guard.CanWrite([], CancellationToken.None));
        Assert.AreEqual(pruned, buffer.Status().Storage!.PrunedLogs);
        Assert.IsFalse(store.PageUsage().CanShrink);
        Assert.IsTrue((await store.QueryAsync(new LogQuery())).Items.Count > 0);
    }

    [TestMethod]
    public async Task SqliteFull_RollsBackBatch_EntersProtection_AndRecovers()
    {
        var disk = new FakeDisk();
        using var host = Host(disk, o => o.StorageCheckInterval = TimeSpan.FromMilliseconds(300));
        await host.StartAsync();
        var store = host.Services.GetRequiredService<SqliteLogStore>();
        using var connection = new SqliteConnection($"Data Source={Database}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE bloater(data BLOB);
            CREATE TRIGGER fill_disk AFTER INSERT ON helper_logs
                BEGIN INSERT INTO bloater VALUES(zeroblob(20971520)); END;
            """;
        command.ExecuteNonQuery();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("FullTest");
        logger.LogError("cannot-commit");
        await Until(() => store.GetStatus().Storage?.PauseReason == "StorageFull");
        Assert.IsTrue(store.GetStatus().Dropped > 0);
        Assert.IsTrue(store.GetStatus().WriteFailures > 0);
        Assert.AreEqual(0, (await store.QueryAsync(new LogQuery { Keyword = "cannot-commit" })).Items.Count);
        command.CommandText = "DROP TRIGGER fill_disk; DROP TABLE bloater;";
        command.ExecuteNonQuery();
        await Until(() => store.GetStatus().Storage?.IsPaused == false);
        logger.LogInformation("committed-after-recovery");
        await host.StopAsync();
        Assert.AreEqual(1, (await store.QueryAsync(new LogQuery { Keyword = "committed-after-recovery" })).Items.Count);
    }

    [TestMethod]
    public async Task ContinuousWrites_SplitBatches_PruneHistory_AndKeepStorageWithinBudget()
    {
        using var host = Host(new FakeDisk());
        await host.StartAsync();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Sustained");
        var payload = new string('x', 4096);
        for (var i = 0; i < 1800; i++) logger.LogInformation("payload {Payload}", payload);
        logger.LogInformation("latest-capacity-marker");
        await host.StopAsync();
        var store = host.Services.GetRequiredService<SqliteLogStore>();
        var status = store.GetStatus();
        Assert.AreEqual(0L, status.Dropped);
        Assert.AreEqual(status.Accepted, status.Persisted);
        Assert.IsTrue(status.Storage!.PrunedLogs > 0);
        var sizes = store.FileSizes();
        Assert.IsTrue(sizes.Database + sizes.Wal + sizes.Shm < status.Storage.MaxStorageBytes);
        Assert.AreEqual(1, (await store.QueryAsync(new LogQuery { Keyword = "latest-capacity-marker" })).Items.Count);
    }

    [TestMethod]
    public async Task OversizedSingleEntry_IsDroppedWithoutClearingHistory_AndCanRecover()
    {
        var (_, store, buffer, guard) = Create();
        guard.Initialize();
        store.Write([Entry("existing-history")]);
        Assert.IsFalse(guard.CanWrite([Entry(new string('x', (int)MiB))], CancellationToken.None));
        Assert.AreEqual("BatchTooLarge", buffer.Status().Storage!.PauseReason);
        Assert.AreEqual(0L, buffer.Status().Storage!.PrunedLogs);
        Assert.IsNotNull(await store.GetAsync(1));
        await Task.Delay(80);
        Assert.IsTrue(guard.CanWrite([], CancellationToken.None));
    }

    [TestMethod]
    public void CapacityOptions_RejectInvalidValues_AndActualMountCanBeMeasured()
    {
        foreach (var change in new Action<HelperLoggingOptions>[]
        {
            o => o.MaxStorageBytes = 1,
            o => o.MinFreeDiskSpaceBytes = -1,
            o => o.DiskSpaceRecoveryMarginBytes = -1,
            o => o.MinFreeDiskSpaceBytes = long.MaxValue,
            o => o.StorageCheckInterval = TimeSpan.Zero
        })
        {
            var options = Options();
            change(options);
            Assert.ThrowsException<ArgumentException>(options.Validate);
        }
        var available = new DiskSpaceProbe().AvailableBytes(_directory);
        Assert.IsTrue(available.HasValue && available.Value > 0);
    }

    private (HelperLoggingOptions Options, SqliteLogStore Store, LogBuffer Buffer, StorageGuard Guard) Create()
    {
        var options = Options();
        var buffer = new LogBuffer(options);
        var store = new SqliteLogStore(options, buffer);
        return (options, store, buffer, new StorageGuard(options, buffer, store, new FakeDisk()));
    }

    private HelperLoggingOptions Options() => new()
    {
        DatabasePath = Database,
        MaxStorageBytes = 16 * MiB,
        MinFreeDiskSpaceBytes = MiB,
        DiskSpaceRecoveryMarginBytes = MiB,
        StorageCheckInterval = TimeSpan.FromMilliseconds(50),
        FlushInterval = TimeSpan.FromMilliseconds(10)
    };

    private IHost Host(FakeDisk disk, Action<HelperLoggingOptions>? configure = null) => new HostBuilder()
        .ConfigureLogging(logging => logging.ClearProviders())
        .ConfigureServices(services =>
        {
            services.AddSingleton<IDiskSpaceProbe>(disk);
            services.AddHelperLogging(o =>
            {
                var settings = Options();
                o.DatabasePath = settings.DatabasePath;
                o.MaxStorageBytes = settings.MaxStorageBytes;
                o.MinFreeDiskSpaceBytes = settings.MinFreeDiskSpaceBytes;
                o.DiskSpaceRecoveryMarginBytes = settings.DiskSpaceRecoveryMarginBytes;
                o.StorageCheckInterval = settings.StorageCheckInterval;
                o.FlushInterval = settings.FlushInterval;
                configure?.Invoke(o);
            });
        }).Build();

    private static void FillTo(SqliteLogStore store, long bytes)
    {
        while (store.PageUsage().UsedBytes < bytes)
            store.Write(Enumerable.Range(0, 20).Select(_ => Entry(new string('x', 4000))).ToArray());
    }

    private static LogEntry Entry(string message) => new()
    {
        TimestampUtc = DateTimeOffset.UtcNow,
        Level = 2,
        Category = "CapacityTest",
        Message = message
    };

    private static async Task Until(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition()) await Task.Delay(10, timeout.Token);
    }

    private sealed class FakeDisk : IDiskSpaceProbe
    {
        private long _free = 1024 * MiB;
        internal long Free { get => Interlocked.Read(ref _free); set => Interlocked.Exchange(ref _free, value); }
        public long? AvailableBytes(string directory) => Free < 0 ? null : Free;
    }
}
