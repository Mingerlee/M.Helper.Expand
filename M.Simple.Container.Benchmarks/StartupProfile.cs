using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

/// <summary>A quick phase diagnostic. Use BenchmarkDotNet for final comparisons.</summary>
internal static class StartupProfile
{
    public static void Run()
    {
        Console.WriteLine("Provider,Build_us,FirstResolve_us,Dispose_us,Iterations");
        foreach (var name in new[] { "Simple", "Microsoft", "Autofac" })
        {
            for (var i = 0; i < 10; i++)
            {
                using var warm = (IDisposable)ResolutionBenchmarks.Build(name);
                ((IServiceProvider)warm).GetRequiredService<ResolutionBenchmarks.Graph>();
            }
            long build = 0, resolve = 0, dispose = 0;
            const int iterations = 100;
            for (var i = 0; i < iterations; i++)
            {
                var start = Stopwatch.GetTimestamp();
                var provider = ResolutionBenchmarks.Build(name);
                var built = Stopwatch.GetTimestamp();
                provider.GetRequiredService<ResolutionBenchmarks.Graph>();
                var resolved = Stopwatch.GetTimestamp();
                ((IDisposable)provider).Dispose();
                var disposed = Stopwatch.GetTimestamp();
                build += built - start;
                resolve += resolved - built;
                dispose += disposed - resolved;
            }
            var factor = 1_000_000d / Stopwatch.Frequency / iterations;
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{name},{build * factor:F3},{resolve * factor:F3},{dispose * factor:F3},{iterations}"));
        }
    }
}
