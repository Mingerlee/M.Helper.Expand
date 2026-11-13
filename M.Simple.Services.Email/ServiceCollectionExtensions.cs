using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Services.Email;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimpleEmail(this IServiceCollection services, Action<MailSettings> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        return services.AddEMailSetup(_ =>
        {
            var settings = new MailSettings();
            configure(settings);
            return settings;
        });
    }

    public static IServiceCollection AddEMailSetup(this IServiceCollection services,
        Func<IServiceProvider, MailSettings> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settingsFactory);
        services.AddTransient(provider => settingsFactory(provider)
            ?? throw new InvalidOperationException("Mail settings factory returned null."));
        services.AddTransient<IEMailService, EMailService>();
        return services;
    }
}
