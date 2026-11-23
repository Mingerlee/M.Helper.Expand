```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26300.9974)
Unknown processor
.NET SDK 8.0.101
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-VYTOAJ : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

InvocationCount=1  IterationCount=1  LaunchCount=5
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=0

```
| Method                      | Provider  | Mean      | Error        | StdDev     | Median    | Allocated |
|---------------------------- |---------- |----------:|-------------:|-----------:|----------:|----------:|
| **BuildFirstResolveAndDispose** | **Autofac**   | **471.00 ms** | **2,567.836 ms** | **666.859 ms** | **180.10 ms** | **117.94 KB** |
| **BuildFirstResolveAndDispose** | **Microsoft** |  **54.61 ms** |     **6.764 ms** |   **1.756 ms** |  **54.85 ms** |   **14.6 KB** |
| **BuildFirstResolveAndDispose** | **Simple**    |  **53.33 ms** |    **20.167 ms** |   **5.237 ms** |  **52.80 ms** |  **12.58 KB** |
