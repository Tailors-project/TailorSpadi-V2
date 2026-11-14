using Microsoft.EntityFrameworkCore;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Abstractions;

/// <summary>
/// The persistence surface handlers use. Infrastructure implements it with EF Core. Tenant-owned sets
/// are filtered by the resolved tenant; code that must read across tenants says so explicitly with
/// IgnoreQueryFilters and an explicit predicate.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantMembership> TenantMemberships { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<AuditLogEntry> AuditLog { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>True when the exception was caused by a unique-constraint violation.</summary>
    bool IsUniqueViolation(Exception exception);

    /// <summary>Forgets all tracked changes so a failed unit of work can be retried from a clean state.</summary>
    void DiscardChanges();

    /// <summary>Immediately revokes a family with one atomic update, safe for concurrent replay detection.</summary>
    Task RevokeRefreshFamilyAsync(Guid familyId, DateTime revokedAtUtc, CancellationToken cancellationToken);
}
