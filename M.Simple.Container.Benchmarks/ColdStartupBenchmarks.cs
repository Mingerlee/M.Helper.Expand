using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Each launch measures one build and resolution, without warming constructor metadata.</summary>
[SimpleJob(RunStrategy.ColdStart, launchCount: 5, warmupCount: 0, iterationCount: 1, invocationCount: 1)]
[MemoryDiagnoser]
public class ColdStartupBenchmarks
{
    [Params("Simple", "Microsoft", "Autofac")]
    public string Provider { get; set; } = "Simple";

    [Benchmark]
    public void BuildFirstResolveAndDispose()
    {
        using var provider = (IDisposable)ResolutionBenchmarks.Build(Provider);
        ((IServiceProvider)provider).GetRequiredService<ResolutionBenchmarks.Graph>();
    }
}
