using Microsoft.Extensions.Hosting;

namespace M.Simple.Logging;

internal sealed class LogWriter(SimpleLoggingOptions options, LogBuffer buffer, SqliteLogStore store, StorageGuard storage)
    : IHostedService, IDisposable
{
    private readonly CancellationTokenSource _abort = new();
    private Task? _worker;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 启动时即验证目录、原生驱动和建表权限，失败时明确阻止错误配置继续运行。
        cancellationToken.ThrowIfCancellationRequested();
        storage.Initialize();
        Volatile.Write(ref buffer.IsWriting, 1);
        _worker = Task.Run(RunAsync, CancellationToken.None);
        return Task.CompletedTask;
    }

    private async Task RunAsync()
    {
        var batch = new List<LogEntry>(options.BatchSize);
        var nextCleanup = DateTimeOffset.MinValue;
        try
        {
            while (!_abort.IsCancellationRequested)
            {
                var estimatedBytes = batch.Sum(StorageGuard.EstimateBytes);
                while (batch.Count < options.BatchSize && buffer.Channel.Reader.TryPeek(out var next))
                {
                    var entryBytes = StorageGuard.EstimateBytes(next);
                    if (batch.Count > 0 && estimatedBytes + entryBytes > options.MaxStorageBytes / 8) break;
                    if (!buffer.Channel.Reader.TryRead(out var entry)) break;
                    batch.Add(entry);
                    estimatedBytes += entryBytes;
                }
                try
                {
                    if (!storage.CanWrite(batch, _abort.Token))
                    {
                        // 保护期间不让旧批次无限重试；已接收和后续日志都明确计入丢弃数量。
                        Interlocked.Add(ref buffer.Dropped, batch.Count);
                        batch.Clear();
                        while (buffer.Channel.Reader.TryRead(out _)) Interlocked.Increment(ref buffer.Dropped);
                        if (buffer.Channel.Reader.Completion.IsCompleted) break;
                        await Task.Delay(options.FlushInterval, _abort.Token);
                        continue;
                    }
                    if (batch.Count > 0)
                    {
                        store.Write(batch);
                        Interlocked.Add(ref buffer.Persisted, batch.Count);
                        Interlocked.Exchange(ref buffer.LastWriteTicks, DateTimeOffset.UtcNow.UtcDateTime.Ticks);
                        batch.Clear();
                        Volatile.Write(ref buffer.LastError, null);
                    }
                    if (DateTimeOffset.UtcNow >= nextCleanup)
                    {
                        // 每次仅删除一个小批次；积压时下一轮继续，避免长事务阻塞日志写入。
                        nextCleanup = store.DeleteExpiredBatch() == 1000
                            ? DateTimeOffset.MinValue : DateTimeOffset.UtcNow.AddHours(1);
                    }
                }
                catch (OperationCanceledException) when (_abort.IsCancellationRequested) { throw; }
                catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 13)
                {
                    Interlocked.Increment(ref buffer.WriteFailures);
                    Volatile.Write(ref buffer.LastError, ex.GetType().Name);
                    storage.StorageFull();
                    Interlocked.Add(ref buffer.Dropped, batch.Count);
                    batch.Clear();
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref buffer.WriteFailures);
                    // 不暴露路径、SQL 或潜在业务内容，也不递归写日志；保留批次等待重试。
                    Volatile.Write(ref buffer.LastError, ex.GetType().Name);
                    await Task.Delay(TimeSpan.FromSeconds(1), _abort.Token);
                    continue;
                }
                if (buffer.Channel.Reader.Completion.IsCompleted) break;
                if (!buffer.Channel.Reader.TryPeek(out _))
                    await Task.Delay(options.FlushInterval, _abort.Token);
            }
        }
        catch (OperationCanceledException) when (_abort.IsCancellationRequested) { }
        finally
        {
            Interlocked.Add(ref buffer.Dropped, batch.Count);
            while (buffer.Channel.Reader.TryRead(out _)) Interlocked.Increment(ref buffer.Dropped);
            Volatile.Write(ref buffer.IsWriting, 0);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        buffer.Channel.Writer.TryComplete();
        if (_worker is null) return;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.ShutdownTimeout);
        try { await _worker.WaitAsync(timeout.Token); }
        catch (OperationCanceledException) { _abort.Cancel(); }
    }

    public void Dispose()
    {
        buffer.Channel.Writer.TryComplete();
        _abort.Cancel();
        // 后台同步 SQLite 调用最多等待连接超时；不阻塞宿主的有界停机流程。
    }
}
