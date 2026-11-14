using Microsoft.EntityFrameworkCore;
using Npgsql;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Infrastructure.Persistence;

/// <summary>
/// The single database context. Tenant-owned sets are filtered by the resolved tenant (and return nothing
/// when no tenant is resolved), and writes to another tenant's records are rejected before they reach the database.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
    : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    // Read by the query filter for each context instance, so every request filters by its own tenant.
    private Guid? CurrentTenantId => tenantContext.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Fails closed: without a resolved tenant the comparison with NULL matches nothing.
        modelBuilder.Entity<TenantMembership>().HasQueryFilter(m => m.TenantId == CurrentTenantId);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnforceTenantWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceTenantWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public bool IsUniqueViolation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }
        }

        return false;
    }

    public void DiscardChanges() => ChangeTracker.Clear();

    public async Task RevokeRefreshFamilyAsync(Guid familyId, DateTime revokedAtUtc, CancellationToken cancellationToken)
    {
        await RefreshTokens.Where(token => token.FamilyId == familyId && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAtUtc, revokedAtUtc), cancellationToken);
    }

    /// <summary>
    /// Inside a tenant request every written tenant-owned record must belong to that tenant. Platform requests
    /// (no tenant) cannot be checked here and instead rely on explicit handler-level predicates.
    /// </summary>
    private void EnforceTenantWrites()
    {
        foreach (var entry in ChangeTracker.Entries<Tenant>().Where(entry => entry.State == EntityState.Modified))
        {
            var subdomain = entry.Property(tenant => tenant.Subdomain);
            if (subdomain.OriginalValue is not null && subdomain.IsModified)
            {
                throw new InvalidOperationException("An assigned business subdomain cannot be changed.");
            }

            if (tenantContext.TenantId is { } currentTenant && entry.Entity.Id != currentTenant)
            {
                throw new InvalidOperationException("A write targeting another tenant was blocked.");
            }
        }

        if (tenantContext.TenantId is not { } tenantId)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            bool written = entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;
            if (!written)
            {
                continue;
            }

            bool movedTenant = entry.State == EntityState.Modified && entry.Property(nameof(ITenantOwned.TenantId)).IsModified;
            if (entry.Entity.TenantId != tenantId || movedTenant)
            {
                throw new InvalidOperationException("A write targeting another tenant was blocked.");
            }
        }
    }
}
