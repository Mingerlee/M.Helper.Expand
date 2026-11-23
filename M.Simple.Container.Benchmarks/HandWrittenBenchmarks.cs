using BenchmarkDotNet.Attributes;

/// <summary>An allocation-equivalent reference for TransientGraph, without service lookup.</summary>
[MemoryDiagnoser]
public class HandWrittenBenchmarks
{
    private readonly ResolutionBenchmarks.Shared _shared = new();

    [Benchmark]
    public ResolutionBenchmarks.Graph TransientGraph()
        => new(new ResolutionBenchmarks.Branch(new ResolutionBenchmarks.Leaf(), _shared),
            new ResolutionBenchmarks.Leaf());
}
