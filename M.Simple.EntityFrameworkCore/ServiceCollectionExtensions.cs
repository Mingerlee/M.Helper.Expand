using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.EntityFrameworkCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimpleEfCore<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services.Any(x => x.ServiceType == typeof(ISimpleDbContext)))
            throw new InvalidOperationException("Only one default Simple EF Core context can be registered.");
        services.AddScoped<ISimpleDbContext, SimpleDbContext<TContext>>();
        services.AddScoped(typeof(IBasicRepository<,>), typeof(EfRepository<,>));
        services.AddScoped(typeof(IBasicIntRepository<>), typeof(EfIntRepository<>));
        services.AddScoped(typeof(IBasicLongRepository<>), typeof(EfLongRepository<>));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        return services;
    }
}
