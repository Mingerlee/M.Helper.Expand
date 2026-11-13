using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Services.Email.Test;

[TestClass]
public sealed class RegistrationTests
{
    [TestMethod]
    public void FactoryUsesCurrentScopeAndCreatesIndependentSettings()
    {
        var services = new ServiceCollection();
        services.AddScoped<Dependency>();
        services.AddEMailSetup(provider => new MailSettings { FromAddress = provider.GetRequiredService<Dependency>().Address });
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        var first = (EMailService)scope.ServiceProvider.GetRequiredService<IEMailService>();
        var second = (EMailService)scope.ServiceProvider.GetRequiredService<IEMailService>();
        Assert.AreNotSame(first, second);
        Assert.AreNotSame(first.Settings, second.Settings);
        Assert.AreEqual(scope.ServiceProvider.GetRequiredService<Dependency>().Address, first.Settings.FromAddress);
    }

    [TestMethod]
    public void ConfigureActionRegistersServiceWithoutTheExpandPackage()
    {
        var services = new ServiceCollection();
        services.AddSimpleEmail(settings => settings.FromAddress = "sender@example.invalid");
        using var provider = services.BuildServiceProvider();
        Assert.AreEqual("sender@example.invalid", ((EMailService)provider.GetRequiredService<IEMailService>()).Settings.FromAddress);
    }

    public sealed class Dependency { public string Address { get; } = Guid.NewGuid() + "@example.invalid"; }
}
