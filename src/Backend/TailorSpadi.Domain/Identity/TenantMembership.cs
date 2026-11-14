using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Domain.Identity;

/// <summary>Joins one user to one tenant with a role. Unique per (TenantId, UserId).</summary>
public sealed class TenantMembership : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public TenantRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }

    public bool IsBackofficeRole => Role is TenantRole.TenantOwner or TenantRole.TenantManager or TenantRole.TenantStaff;
}
