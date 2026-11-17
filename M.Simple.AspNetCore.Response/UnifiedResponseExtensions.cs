using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace M.Simple.AspNetCore.Response;

public static class UnifiedResponseExtensions
{
    /// <summary>Registers MVC response wrapping, automatic validation and an exception handler.</summary>
    public static IServiceCollection AddUnifiedResponse(
        this IServiceCollection services, Action<UnifiedResponseOptions>? configure = null)
    {
        services.AddOptions<UnifiedResponseOptions>();
        if (configure is not null) services.Configure(configure);
        services.AddControllers();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<MvcOptions>, ConfigureResponseMvcOptions>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ApiBehaviorOptions>, ConfigureResponseApiBehaviorOptions>());
        services.AddExceptionHandler<UnifiedResponseExceptionHandler>();
        return services;
    }

    /// <summary>Enables exception and empty error responses. Call before authentication and controller endpoints.</summary>
    public static IApplicationBuilder UseUnifiedResponse(this IApplicationBuilder app)
    {
        if (app.Properties.ContainsKey("M.Simple.UnifiedResponse.Enabled")) return app;
        app.Properties["M.Simple.UnifiedResponse.Enabled"] = true;
        app.ApplicationServices.GetRequiredService<IOptions<UnifiedResponseOptions>>();
        app.UseMiddleware<UnifiedResponseExceptionMiddleware>();
        app.UseStatusCodePages(async context =>
        {
            var http = context.HttpContext;
            if (http.GetEndpoint()?.Metadata.GetMetadata<SkipUnifiedResponseAttribute>() is not null) return;
            var options = http.RequestServices.GetRequiredService<IOptions<UnifiedResponseOptions>>().Value;
            var json = http.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
            await http.Response.WriteAsJsonAsync(options.Create(http.Response.StatusCode), json, http.RequestAborted);
        });
        return app;
    }
}

internal sealed class ConfigureResponseMvcOptions : IConfigureOptions<MvcOptions>
{
    public void Configure(MvcOptions options) => options.Filters.Add<UnifiedResponseFilter>();
}

internal sealed class ConfigureResponseApiBehaviorOptions(IOptions<UnifiedResponseOptions> options) : IConfigureOptions<ApiBehaviorOptions>
{
    public void Configure(ApiBehaviorOptions behavior)
    {
        var originalFactory = behavior.InvalidModelStateResponseFactory;
        behavior.InvalidModelStateResponseFactory = context =>
        {
            if (context.ActionDescriptor.EndpointMetadata.OfType<SkipUnifiedResponseAttribute>().Any())
                return originalFactory(context);
            var errors = context.ModelState.Where(item => item.Value?.Errors.Count > 0)
                .ToDictionary(item => item.Key, item => item.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "参数格式不正确" : error.ErrorMessage)
                    .ToArray());
            return new BadRequestObjectResult(options.Value.Create(400, errors, options.Value.ValidationErrorMessage));
        };
    }
}
