using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace M.Simple.AspNetCore.Response.Swagger;

public static class UnifiedResponseSwaggerExtensions
{
    /// <summary>Adds response schemas to an existing Swashbuckle 6.x Swagger registration.</summary>
    public static IServiceCollection AddUnifiedResponseSwagger(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<SwaggerGenOptions>, ConfigureResponseSwaggerOptions>());
        return services;
    }
}

internal sealed class ConfigureResponseSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options) => options.OperationFilter<UnifiedResponseOperationFilter>();
}
