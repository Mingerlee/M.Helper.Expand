using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.Container.Test;

[TestClass]
public class HostingTests
{
    [TestMethod]
    public async Task MinimalApiSupportsOptionsLoggingKeyedServicesAndRequestScopes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Host.UseSimpleContainer();
        builder.Services.AddScoped<RequestIdentity>();
        builder.Services.AddKeyedSingleton<string>("greeting", "hello");
        await using var app = builder.Build();
        Assert.IsInstanceOfType(app.Services, typeof(SimpleServiceProvider));
        app.MapGet("/scope", (RequestIdentity identity, HttpContext context, [FromKeyedServices("greeting")] string greeting)
            => new ScopeResponse(identity.Id, context.RequestServices.GetRequiredService<RequestIdentity>().Id, greeting));
        await app.StartAsync();
        using var client = app.GetTestClient();
        var a = (await client.GetFromJsonAsync<ScopeResponse>("/scope"))!;
        var b = (await client.GetFromJsonAsync<ScopeResponse>("/scope"))!;
        Assert.AreEqual(a.First, a.Second);
        Assert.AreNotEqual(a.First, b.First);
        Assert.AreEqual("hello", a.Greeting);
    }

    [TestMethod]
    public async Task MvcControllerActivationWorks()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Host.UseSimpleContainer();
        builder.Services.AddScoped<RequestIdentity>();
        builder.Services.AddControllers().AddApplicationPart(typeof(ContainerProbeController).Assembly);
        await using var app = builder.Build();
        app.MapControllers();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var response = await client.GetAsync("/container-probe");
        response.EnsureSuccessStatusCode();
        Assert.AreNotEqual(Guid.Empty, await response.Content.ReadFromJsonAsync<Guid>());
    }

    [TestMethod]
    public async Task StandardHostBuilderUsesFactoryAndHostedServices()
    {
        using var host = new HostBuilder().UseSimpleContainer()
            .ConfigureServices(services => services.AddHostedService<ProbeHostedService>()).Build();
        await host.StartAsync();
        Assert.IsInstanceOfType(host.Services, typeof(SimpleServiceProvider));
        Assert.IsTrue(((ProbeHostedService)host.Services.GetServices<IHostedService>().Single()).Started);
        await host.StopAsync();
    }

    public class RequestIdentity { public Guid Id { get; } = Guid.NewGuid(); }
    public record ScopeResponse(Guid First, Guid Second, string Greeting);
    public class ProbeHostedService : IHostedService
    {
        public bool Started;
        public Task StartAsync(CancellationToken token) { Started = true; return Task.CompletedTask; }
        public Task StopAsync(CancellationToken token) => Task.CompletedTask;
    }
}

[ApiController]
[Route("container-probe")]
public class ContainerProbeController(HostingTests.RequestIdentity identity) : ControllerBase
{
    [HttpGet]
    public Guid Get() => identity.Id;
}
