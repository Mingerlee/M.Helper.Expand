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
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        return services;
    }
}
