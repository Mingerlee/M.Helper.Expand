using System.Text;

namespace M.Simple.Logging;

internal interface IDiskSpaceProbe
{
    long? AvailableBytes(string directory);
}

internal sealed class DiskSpaceProbe : IDiskSpaceProbe
{
    public long? AvailableBytes(string directory)
    {
        // Linux 按最长挂载点匹配，避免把 Docker 挂载卷误认为容器根文件系统。
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var drive = DriveInfo.GetDrives().OrderByDescending(d => d.Name.Length).FirstOrDefault(d =>
        {
            var root = Path.TrimEndingDirectorySeparator(d.Name);
            return directory.Equals(root, comparison) ||
                directory.StartsWith(Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar, comparison);
        });
        return drive?.AvailableFreeSpace;
    }
}

/// <summary>由单个后台写入器调用；容量维护不进入业务线程，不执行完整 VACUUM。</summary>
internal sealed class StorageGuard(SimpleLoggingOptions options, LogBuffer buffer,
    SqliteLogStore store, IDiskSpaceProbe disk)
{
    private bool _initialized;
    private bool _lowDisk;
    private long _pruned;
    private DateTimeOffset _nextCheck;
    private DateTimeOffset _nextMaintenance;

    internal void Initialize()
    {
        if (!DiskReady(out var available)) return;
        store.Initialize();
        _initialized = true;
        Publish(available, null);
    }

    internal bool CanWrite(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken)
    {
        if (buffer.Storage?.IsPaused == true && DateTimeOffset.UtcNow < _nextCheck) return false;
        try
        {
            if (!DiskReady(out var available)) return false;
            if (!_initialized)
            {
                store.Initialize();
                _initialized = true;
            }
            var reserve = batch.Count == 0 ? 0 : 65_536 + batch.Sum(EstimateBytes);
            // 不为单条超大记录清空历史日志。正常批次已在写入器按字节预算拆小。
            if (reserve > options.MaxStorageBytes / 4) return Pause(available, "BatchTooLarge");
            if (available!.Value - reserve < options.MinFreeDiskSpaceBytes)
            {
                _lowDisk = true;
                return Pause(available, "LowDiskSpace");
            }
            var sizes = store.FileSizes();
            var mainLimit = options.MaxStorageBytes * 3 / 4;
            var target = mainLimit * 4 / 5;
            if (DateTimeOffset.UtcNow >= _nextMaintenance ||
                sizes.Database + reserve / 2 >= mainLimit * 9 / 10 ||
                sizes.Database + sizes.Wal + sizes.Shm + reserve >= options.MaxStorageBytes * 9 / 10)
            {
                _nextMaintenance = DateTimeOffset.UtcNow.Add(options.StorageCheckInterval);
                var checkpointReady = store.Checkpoint();
                var usage = store.PageUsage();
                sizes = store.FileSizes();
                for (var pass = 0; pass < 10; pass++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var liveTooLarge = usage.UsedBytes + reserve / 2 > target;
                    var filesTooLarge = sizes.Database + sizes.Wal + sizes.Shm + reserve > options.MaxStorageBytes * 4 / 5;
                    if (!liveTooLarge && !filesTooLarge) break;
                    // 被读事务占用的 WAL 无法回收，先暂停，避免继续删除仍然无法释放的历史日志。
                    if (!checkpointReady) return Pause(available, "CheckpointBusy");
                    if (liveTooLarge)
                    {
                        var deleted = store.DeleteOldestBatch();
                        _pruned += deleted;
                        if (deleted == 0) break;
                    }
                    else if (!usage.CanShrink || usage.FreePages == 0) break;
                    if (!DiskReady(out available)) return false;
                    if (usage.CanShrink) store.ReclaimPages();
                    checkpointReady = store.Checkpoint();
                    usage = store.PageUsage();
                    sizes = store.FileSizes();
                }
                if (usage.UsedBytes + reserve / 2 >= mainLimit) return Pause(available, "StorageLimit");
            }
            sizes = store.FileSizes();
            if (sizes.Database + sizes.Wal + sizes.Shm + reserve >= options.MaxStorageBytes)
                return Pause(available, "StorageLimit");
            Publish(available, null);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            Volatile.Write(ref buffer.LastError, ex.GetType().Name);
            return Pause(null, "StorageCheckFailed");
        }
    }

    internal void StorageFull()
    {
        // SQLITE_FULL 包含磁盘满和主文件页数限额；丢弃未提交批次，稍后重新检查恢复。
        Pause(null, "StorageFull");
        _nextMaintenance = DateTimeOffset.MinValue;
    }

    private bool DiskReady(out long? available)
    {
        try { available = disk.AvailableBytes(Path.GetDirectoryName(store.DatabasePath)!); }
        catch (Exception) { available = null; }
        if (available is null || available < 0) return Pause(null, "DiskSpaceUnavailable");
        var threshold = options.MinFreeDiskSpaceBytes + (_lowDisk ? options.DiskSpaceRecoveryMarginBytes : 0);
        if (available < threshold)
        {
            _lowDisk = true;
            return Pause(available, "LowDiskSpace");
        }
        _lowDisk = false;
        return true;
    }

    private bool Pause(long? available, string reason)
    {
        _nextCheck = DateTimeOffset.UtcNow.Add(options.StorageCheckInterval);
        Publish(available, reason);
        return false;
    }

    private void Publish(long? available, string? reason)
    {
        // 文件检查失败时仍发布暂停状态；不能因状态展示异常而继续写盘。
        var old = Volatile.Read(ref buffer.Storage);
        var sizes = (Database: old?.DatabaseBytes ?? 0, Wal: old?.WalBytes ?? 0, Shm: old?.SharedMemoryBytes ?? 0);
        try { sizes = store.FileSizes(); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        Volatile.Write(ref buffer.Storage, new LogStorageStatus(sizes.Database, sizes.Wal, sizes.Shm,
            options.MaxStorageBytes, available, options.MinFreeDiskSpaceBytes,
            reason is not null, reason, _pruned, DateTimeOffset.UtcNow));
    }

    internal static long EstimateBytes(LogEntry entry)
    {
        // 为 UTF-8 行数据、索引页、页拆分和 WAL 提供保守的事务增长余量。
        var bytes = new[] { entry.Category, entry.EventName, entry.MessageTemplate, entry.Message, entry.Exception,
            entry.PropertiesJson, entry.ScopesJson, entry.TraceId, entry.SpanId, entry.ApplicationName, entry.InstanceId }
            .Sum(text => text is null ? 0L : Encoding.UTF8.GetByteCount(text));
        return (bytes + 4096) * 4;
    }
}
