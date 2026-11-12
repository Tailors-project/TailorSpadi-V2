using TailorSpadi.Application.Tenancy;

namespace TailorSpadi.Application.Features.Tenants;

public static class LogoUrls
{
    /// <summary>A same-origin, cache-busting logo URL, or null when the tenant has no logo.</summary>
    public static string? For(TenantInfo tenant)
    {
        if (tenant.LogoStorageKey is not { } key)
        {
            return null;
        }

        string version = Path.GetFileNameWithoutExtension(key);
        return "/api/tenant/current/logo?v=" + Uri.EscapeDataString(version);
    }
}
