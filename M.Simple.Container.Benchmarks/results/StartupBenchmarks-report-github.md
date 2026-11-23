```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26300.9974)
Unknown processor
.NET SDK 8.0.101
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-QXFOGL : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=8  LaunchCount=1  WarmupCount=5

```
| Method                      | Provider  | Mean         | Error       | StdDev     | Gen0   | Gen1   | Allocated |
|---------------------------- |---------- |-------------:|------------:|-----------:|-------:|-------:|----------:|
| **BuildFirstResolveAndDispose** | **Autofac**   |    **54.287 μs** |  **31.6380 μs** | **16.5473 μs** | **6.5918** | **0.4883** |  **41.37 KB** |
| **BuildFirstResolveAndDispose** | **Microsoft** |     **9.324 μs** |   **0.8742 μs** |  **0.3882 μs** | **1.4725** | **0.0305** |   **9.06 KB** |
| **BuildFirstResolveAndDispose** | **Simple**    | **1,311.792 μs** | **132.9948 μs** | **69.5589 μs** | **5.8594** | **3.9063** |  **36.01 KB** |
