using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Services.DependencyInjection.Test;

[TestClass]
public sealed class RegistrationTests
{
    [TestMethod]
    public void ScanningIsIdempotentAndAutowiredUsesCurrentScope()
    {
        var services = new ServiceCollection();
        services.AddAppServices(typeof(RegistrationTests).Assembly);
        services.AddAppServices(typeof(RegistrationTests).Assembly);
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(IGreeting)));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        Assert.ThrowsException<InvalidOperationException>(() => provider.GetRequiredService<AutowiredService>());
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var a = new Consumer();
        var b = new Consumer();
        first.ServiceProvider.GetRequiredService<AutowiredService>().Autowired(a);
        second.ServiceProvider.GetRequiredService<AutowiredService>().Autowired(b);
        Assert.AreSame(first.ServiceProvider.GetRequiredService<IGreeting>(), a.Greeting);
        Assert.AreNotSame(a.Greeting, b.Greeting);
    }

    public interface IGreeting { }
    [AppService(ServiceType = typeof(IGreeting), Lifetime = ServiceLifetime.Scoped)]
    public sealed class Greeting : IGreeting { }
    public sealed class Consumer { [Autowired] public IGreeting Greeting { get; set; } }
}
