using TailorSpadi.Application.Tenancy;

namespace TailorSpadi.Application.Abstractions;

public interface ITenantResolver
{
    Task<TenantResolution> ResolveAsync(string? host, CancellationToken cancellationToken);
}

public interface ITenantCache
{
    /// <summary>Removes cached resolution results for a subdomain after the tenant changes.</summary>
    void Invalidate(string subdomain);
}
