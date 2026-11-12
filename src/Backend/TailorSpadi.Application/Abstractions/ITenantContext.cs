using TailorSpadi.Application.Tenancy;

namespace TailorSpadi.Application.Abstractions;

/// <summary>
/// The tenant resolved from the accepted request hostname, or null for a platform host.
/// A tenant is never selected from client-supplied identifiers.
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }

    /// <summary>Public-safe details of the resolved active tenant, or null on a platform host.</summary>
    TenantInfo? Tenant { get; }
}
