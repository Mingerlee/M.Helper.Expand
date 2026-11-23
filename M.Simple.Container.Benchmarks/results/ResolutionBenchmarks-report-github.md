```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26300.9974)
Unknown processor
.NET SDK 8.0.101
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-QXFOGL : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=8  LaunchCount=1  WarmupCount=5

```
| Method               | Provider  | Mean        | Error      | StdDev     | Gen0   | Allocated |
|--------------------- |---------- |------------:|-----------:|-----------:|-------:|----------:|
| **Singleton**            | **Autofac**   |   **337.05 ns** |  **56.240 ns** |  **29.415 ns** | **0.1287** |     **808 B** |
| TransientGraph       | Autofac   | 2,136.27 ns | 741.107 ns | 387.613 ns | 0.4158 |    2632 B |
| ScopedCached         | Autofac   |   673.23 ns | 321.078 ns | 142.561 ns | 0.1287 |     808 B |
| ScopeGraphAndDispose | Autofac   | 8,055.79 ns | 207.058 ns | 108.295 ns | 1.0834 |    6832 B |
| **Singleton**            | **Microsoft** |    **48.43 ns** |   **1.606 ns** |   **0.840 ns** |      **-** |         **-** |
| TransientGraph       | Microsoft |    84.14 ns |  14.503 ns |   7.585 ns | 0.0178 |     112 B |
| ScopedCached         | Microsoft |    65.58 ns |  13.824 ns |   7.230 ns |      - |         - |
| ScopeGraphAndDispose | Microsoft |   310.52 ns |  55.694 ns |  29.129 ns | 0.0863 |     544 B |
| **Singleton**            | **Simple**    |    **77.90 ns** |  **11.983 ns** |   **6.267 ns** |      **-** |         **-** |
| TransientGraph       | Simple    |   223.69 ns | 114.004 ns |  59.626 ns | 0.0176 |     112 B |
| ScopedCached         | Simple    |    98.52 ns |  23.602 ns |  12.345 ns |      - |         - |
| ScopeGraphAndDispose | Simple    |   631.49 ns | 221.622 ns | 115.913 ns | 0.1192 |     752 B |
