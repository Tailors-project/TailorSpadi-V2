using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Infrastructure.Persistence;
using TailorSpadi.Infrastructure.Security;
using TailorSpadi.Infrastructure.Seeding;
using TailorSpadi.Infrastructure.Storage;
using TailorSpadi.Infrastructure.Tenancy;

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
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
        services.AddSingleton<IPublicUrlBuilder, PublicUrlBuilder>();
        services.AddSingleton<TenantResolver>();
        services.AddSingleton<ITenantResolver>(provider => provider.GetRequiredService<TenantResolver>());
        services.AddSingleton<ITenantCache>(provider => provider.GetRequiredService<TenantResolver>());
        services.AddHostedService<AdminSeedHostedService>();
        return services;
    }
}
