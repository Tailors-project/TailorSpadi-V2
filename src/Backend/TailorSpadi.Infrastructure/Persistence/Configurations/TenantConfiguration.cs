using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.BusinessName).HasMaxLength(100).IsRequired();
        builder.Property(tenant => tenant.Description).HasMaxLength(1000).IsRequired();
        builder.Property(tenant => tenant.Subdomain).HasMaxLength(63);
        builder.Property(tenant => tenant.PreferredSlugBase).HasMaxLength(59);
        builder.Property(tenant => tenant.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(tenant => tenant.Phone).HasMaxLength(30).IsRequired();
        builder.Property(tenant => tenant.WhatsApp).HasMaxLength(30).IsRequired();
        builder.Property(tenant => tenant.Email).HasMaxLength(254).IsRequired();
        builder.Property(tenant => tenant.Address).HasMaxLength(200).IsRequired();
        builder.Property(tenant => tenant.City).HasMaxLength(80).IsRequired();
        builder.Property(tenant => tenant.State).HasMaxLength(80).IsRequired();
        builder.Property(tenant => tenant.Country).HasMaxLength(80).IsRequired();
        builder.Property(tenant => tenant.LogoStorageKey).HasMaxLength(200);
        builder.Property(tenant => tenant.PrimaryColor).HasMaxLength(7).IsRequired();
        builder.Property(tenant => tenant.SecondaryColor).HasMaxLength(7).IsRequired();
        builder.Property(tenant => tenant.ConcurrencyStamp).IsConcurrencyToken();

        // Unique subdomain; the column is NULL until onboarding completes (NULLs do not collide).
        builder.HasIndex(tenant => tenant.Subdomain).IsUnique();

        // One unfinished draft per user: the link is cleared when onboarding completes.
        builder.HasIndex(tenant => tenant.DraftOwnerUserId).IsUnique();

        builder.HasOne<User>().WithMany().HasForeignKey(tenant => tenant.DraftOwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(tenant => tenant.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
