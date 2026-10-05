using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TailorSpadi.Infrastructure.Persistence;

public sealed class PostgresHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
    }
}
