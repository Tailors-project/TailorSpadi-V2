namespace TailorSpadi.Domain.Tenancy;

/// <summary>
/// Marks a record that belongs to exactly one tenant. The persistence layer filters these
/// records by the resolved tenant and rejects writes that target another tenant.
/// </summary>
public interface ITenantOwned
{
    Guid TenantId { get; }
}
