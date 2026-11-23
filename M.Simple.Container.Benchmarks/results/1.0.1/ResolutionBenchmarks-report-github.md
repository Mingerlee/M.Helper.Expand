```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26300.9974)
Unknown processor
.NET SDK 8.0.101
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-XZVOQO : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=6  LaunchCount=1  WarmupCount=3

```
| Method               | Provider  | Mean        | Error      | StdDev     | Gen0   | Allocated |
|--------------------- |---------- |------------:|-----------:|-----------:|-------:|----------:|
| **Singleton**            | **Autofac**   |   **665.19 ns** |  **33.693 ns** |  **12.015 ns** | **0.1287** |     **808 B** |
| TransientGraph       | Autofac   | 3,886.89 ns |  94.105 ns |  33.559 ns | 0.4120 |    2632 B |
| ScopedCached         | Autofac   |   668.51 ns |  23.321 ns |   6.057 ns | 0.1287 |     808 B |
| ScopeGraphAndDispose | Autofac   | 8,280.26 ns | 429.121 ns | 153.029 ns | 1.0834 |    6832 B |
| **Singleton**            | **Microsoft** |    **48.54 ns** |   **2.265 ns** |   **0.808 ns** |      **-** |         **-** |
| TransientGraph       | Microsoft |   110.86 ns |   1.578 ns |   0.563 ns | 0.0176 |     112 B |
| ScopedCached         | Microsoft |    98.94 ns |   2.887 ns |   1.030 ns |      - |         - |
| ScopeGraphAndDispose | Microsoft |   616.08 ns |  20.120 ns |   7.175 ns | 0.0858 |     544 B |
| **Singleton**            | **Simple**    |   **138.79 ns** |   **2.808 ns** |   **1.001 ns** |      **-** |         **-** |
| TransientGraph       | Simple    |   372.25 ns |  10.471 ns |   3.734 ns | 0.0176 |     112 B |
| ScopedCached         | Simple    |   144.38 ns |   4.900 ns |   1.747 ns |      - |         - |
| ScopeGraphAndDispose | Simple    | 1,201.66 ns |  45.607 ns |  16.264 ns | 0.1183 |     752 B |
