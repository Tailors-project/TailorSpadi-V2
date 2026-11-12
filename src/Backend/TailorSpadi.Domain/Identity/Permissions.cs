namespace TailorSpadi.Domain.Identity;

/// <summary>Static permission catalog. A database-driven permission engine is deferred to a later module.</summary>
public static class Permissions
{
    public const string ManageBusiness = "ManageBusiness";

    private static readonly Dictionary<TenantRole, string[]> ByRole = new()
    {
        [TenantRole.TenantOwner] = [ManageBusiness],
        [TenantRole.TenantManager] = [],
        [TenantRole.TenantStaff] = [],
        [TenantRole.Customer] = []
    };

    public static bool Has(TenantRole role, string permission) =>
        ByRole.TryGetValue(role, out string[]? permissions) && Array.IndexOf(permissions, permission) >= 0;
}
