using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TailorSpadi.Infrastructure.Persistence;

namespace TailorSpadi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            string connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Configure ConnectionStrings:Postgres using user secrets or environment variables.");

            options.UseNpgsql(connectionString);
        });

        services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);
        return services;
    }
}
