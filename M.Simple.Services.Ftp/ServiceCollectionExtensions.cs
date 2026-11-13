using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Services.Ftp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimpleFtp(this IServiceCollection services, Action<FtpClientSettings> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        return services.AddFtpClientSetup(_ =>
        {
            var settings = new FtpClientSettings();
            configure(settings);
            return settings;
        });
    }

    public static IServiceCollection AddFtpClientSetup(this IServiceCollection services,
        Func<IServiceProvider, FtpClientSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settingsFactory);
        services.AddTransient(provider => settingsFactory(provider)
            ?? throw new InvalidOperationException("FTP settings factory returned null."));
        services.AddTransient<IFtpClientService, FtpClientService>();
        return services;
    }
}
