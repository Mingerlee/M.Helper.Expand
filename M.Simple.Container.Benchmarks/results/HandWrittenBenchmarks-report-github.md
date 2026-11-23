```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26300.9974)
Unknown processor
.NET SDK 8.0.101
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-QXFOGL : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=8  LaunchCount=1  WarmupCount=5

```
| Method         | Mean     | Error    | StdDev   | Gen0   | Allocated |
|--------------- |---------:|---------:|---------:|-------:|----------:|
| TransientGraph | 25.55 ns | 13.90 ns | 6.170 ns | 0.0178 |     112 B |
