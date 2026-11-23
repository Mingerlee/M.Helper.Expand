```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26300.9974)
Unknown processor
.NET SDK 8.0.101
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-XZVOQO : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=6  LaunchCount=1  WarmupCount=3

```
| Method                      | Provider  | Mean      | Error      | StdDev    | Gen0   | Gen1   | Allocated |
|---------------------------- |---------- |----------:|-----------:|----------:|-------:|-------:|----------:|
| **BuildAndDispose**             | **Autofac**   | **64.259 μs** |  **2.8303 μs** | **0.7350 μs** | **5.6152** | **0.4883** |  **35.76 KB** |
| BuildFirstResolveAndDispose | Autofac   | 83.449 μs | 25.6240 μs | 6.6545 μs | 6.3477 | 0.4883 |  41.39 KB |
| **BuildAndDispose**             | **Microsoft** | **10.354 μs** |  **0.2981 μs** | **0.1063 μs** | **1.3885** | **0.0305** |   **8.55 KB** |
| BuildFirstResolveAndDispose | Microsoft | 11.605 μs |  1.2473 μs | 0.4448 μs | 1.4648 | 0.0305 |   9.06 KB |
| **BuildAndDispose**             | **Simple**    |  **9.575 μs** |  **0.3041 μs** | **0.1084 μs** | **1.2207** | **0.0153** |   **7.57 KB** |
| BuildFirstResolveAndDispose | Simple    |  9.999 μs |  0.5473 μs | 0.1952 μs | 1.2665 | 0.0153 |   7.84 KB |
