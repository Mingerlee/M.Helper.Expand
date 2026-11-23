```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26300.9974)
Unknown processor
.NET SDK 8.0.101
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-RXDAJS : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

InvocationCount=1  IterationCount=1  LaunchCount=5
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=0

```
| Method                      | Provider  | Mean      | Error       | StdDev     | Median    | Allocated |
|---------------------------- |---------- |----------:|------------:|-----------:|----------:|----------:|
| **BuildFirstResolveAndDispose** | **Autofac**   | **469.79 ms** | **2,620.24 ms** | **680.468 ms** | **173.54 ms** | **109.71 KB** |
| **BuildFirstResolveAndDispose** | **Microsoft** |  **50.31 ms** |    **17.31 ms** |   **4.496 ms** |  **52.25 ms** |   **14.6 KB** |
| **BuildFirstResolveAndDispose** | **Simple**    |  **67.52 ms** |    **32.95 ms** |   **8.558 ms** |  **73.14 ms** |  **36.94 KB** |
