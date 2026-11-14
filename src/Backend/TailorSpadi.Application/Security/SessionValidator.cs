using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Security;

/// <summary>The identity facts carried by a validated access token.</summary>
public sealed record SessionClaims(Guid UserId, AuthScope Scope, Guid? TenantId, string? Role, string SecurityStamp);

/// <summary>
/// Checks, on every protected request, that a cryptographically valid token still describes a live session:
/// the right host, an active user, the current security stamp and an active membership.
/// </summary>
public sealed class SessionValidator(IAppDbContext db, ITenantContext tenantContext)
{
    /// <summary>Returns null when the session is valid, otherwise a short internal reason (never shown to users).</summary>
    public async Task<string?> ValidateAsync(SessionClaims claims, CancellationToken cancellationToken)
    {
        if (claims.Scope == AuthScope.Tenant)
        {
            if (tenantContext.TenantId is not { } hostTenant || claims.TenantId != hostTenant)
            {
                return "tenant-mismatch";
            }
        }
        else if (tenantContext.TenantId is not null || claims.TenantId is not null)
        {
            return "scope-host-mismatch";
        }

        User? user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == claims.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.SecurityStamp != claims.SecurityStamp)
        {
            return "user-invalid";
        }

        if ((claims.Scope == AuthScope.Admin) != user.IsPlatformAdmin)
        {
            return "admin-separation";
        }

        if (claims.Scope == AuthScope.Admin)
        {
            return claims.Role == "PlatformAdmin" ? null : "admin-role";
        }

        if (claims.Scope == AuthScope.Platform)
        {
            return null;
        }

        TenantMembership? membership = await db.TenantMemberships.AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.TenantId == claims.TenantId && candidate.UserId == claims.UserId,
                cancellationToken);
        if (membership is null || !membership.IsActive || membership.Role.ToString() != claims.Role)
        {
            return "membership-invalid";
        }

        return null;
    }
}
