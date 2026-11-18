namespace M.Simple.Caching;

internal sealed class CacheCounters
{
    private readonly DateTimeOffset _started = DateTimeOffset.UtcNow;
    private long _hits, _misses, _loads, _failures, _writes, _removes;
    internal void Read(bool hit) { if (hit) Interlocked.Increment(ref _hits); else Interlocked.Increment(ref _misses); }
    internal void Load() => Interlocked.Increment(ref _loads);
    internal void Failure() => Interlocked.Increment(ref _failures);
    internal void Write() => Interlocked.Increment(ref _writes);
    internal void Remove() => Interlocked.Increment(ref _removes);
    internal CacheStatistics Snapshot() => new(_started, Interlocked.Read(ref _hits), Interlocked.Read(ref _misses),
        Interlocked.Read(ref _loads), Interlocked.Read(ref _failures), Interlocked.Read(ref _writes), Interlocked.Read(ref _removes));
}
