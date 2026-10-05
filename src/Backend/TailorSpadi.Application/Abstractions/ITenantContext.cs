namespace TailorSpadi.Application.Abstractions;

/// <summary>A validated tenant, or null for a tenant-independent request.</summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
}
