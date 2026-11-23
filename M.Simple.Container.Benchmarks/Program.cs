using Autofac;
using Autofac.Extensions.DependencyInjection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using M.Simple.Container;

if (args.Contains("--startup-profile"))
{
    StartupProfile.Run();
    return;
}
BenchmarkSwitcher.FromAssembly(typeof(ResolutionBenchmarks).Assembly).Run(args);

[MemoryDiagnoser]
public class ResolutionBenchmarks
{
    [Params("Simple", "Microsoft", "Autofac")]
    public string Provider { get; set; } = "Simple";
    private IServiceProvider _provider = null!;
    private IServiceScope _scope = null!;

    public static ServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Shared>();
        services.AddTransient<Leaf>();
        services.AddTransient<Branch>();
        services.AddTransient<Graph>();
        services.AddScoped<RequestGraph>();
        return services;
    }

    public static IServiceProvider Build(string provider)
    {
        var services = Services();
        if (provider == "Simple") return services.BuildSimpleServiceProvider();
        if (provider == "Microsoft") return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var builder = new ContainerBuilder();
        builder.Populate(services);
        return new AutofacServiceProvider(builder.Build());
    }

    [GlobalSetup]
    public void Setup()
    {
        _provider = Build(Provider);
        _scope = _provider.CreateScope();
        // Warm every plan. Cold startup is measured separately.
        _provider.GetRequiredService<Shared>();
        _provider.GetRequiredService<Graph>();
        _scope.ServiceProvider.GetRequiredService<RequestGraph>();
    }

    [Benchmark] public object Singleton() => _provider.GetRequiredService<Shared>();
    [Benchmark] public object TransientGraph() => _provider.GetRequiredService<Graph>();
    [Benchmark] public object ScopedCached() => _scope.ServiceProvider.GetRequiredService<RequestGraph>();
    [Benchmark]
    public void ScopeGraphAndDispose()
    {
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<RequestGraph>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
        ((IDisposable)_provider).Dispose();
    }

    public sealed class Shared;
    public sealed class Leaf;
    public sealed class Branch(Leaf leaf, Shared shared)
    {
        public Leaf Leaf { get; } = leaf;
        public Shared Shared { get; } = shared;
    }
    public sealed class Graph(Branch branch, Leaf leaf)
    {
        public Branch Branch { get; } = branch;
        public Leaf Leaf { get; } = leaf;
    }
    public sealed class RequestGraph(Graph graph, Shared shared) : IDisposable
    {
        public Graph Graph { get; } = graph;
        public Shared Shared { get; } = shared;
        public void Dispose() { }
    }
}

[MemoryDiagnoser]
public class StartupBenchmarks
{
    [Params("Simple", "Microsoft", "Autofac")]
    public string Provider { get; set; } = "Simple";

    [Benchmark]
    public void BuildAndDispose()
    {
        using var provider = (IDisposable)ResolutionBenchmarks.Build(Provider);
    }

    [Benchmark]
    public void BuildFirstResolveAndDispose()
    {
        using var provider = (IDisposable)ResolutionBenchmarks.Build(Provider);
        ((IServiceProvider)provider).GetRequiredService<ResolutionBenchmarks.Graph>();
    }
}
