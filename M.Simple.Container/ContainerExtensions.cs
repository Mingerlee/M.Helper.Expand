using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace M.Simple.Container;

public static class ContainerExtensions
{
    public static SimpleServiceProvider BuildSimpleServiceProvider(
        this IServiceCollection services, Action<SimpleContainerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new SimpleContainerOptions();
        configure?.Invoke(options);
        return new SimpleServiceProvider(services, options);
    }

    public static IHostBuilder UseSimpleContainer(this IHostBuilder host,
        Action<SimpleContainerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(host);
        return host.UseServiceProviderFactory(new SimpleServiceProviderFactory(configure));
    }
}

/// <summary>Uses the standard service collection as the container builder.</summary>
public sealed class SimpleServiceProviderFactory(Action<SimpleContainerOptions>? configure = null)
    : IServiceProviderFactory<IServiceCollection>
{
    public IServiceCollection CreateBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }

    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        => containerBuilder.BuildSimpleServiceProvider(configure);
}
