using TailorSpadi.Application.Abstractions;

namespace TailorSpadi.Application.Tenancy;

/// <summary>Per-request tenant context, populated once by the tenant resolution middleware.</summary>
public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId => Tenant?.Id;
    public TenantInfo? Tenant { get; private set; }

    public void Set(TenantInfo? tenant) => Tenant = tenant;
}
