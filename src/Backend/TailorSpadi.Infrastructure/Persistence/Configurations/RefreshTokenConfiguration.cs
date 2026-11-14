using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", table => table.HasCheckConstraint(
            "CK_RefreshTokens_Scope", "(\"Scope\" = 'Tenant' AND \"TenantId\" IS NOT NULL) OR (\"Scope\" IN ('Platform', 'Admin') AND \"TenantId\" IS NULL)"));
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Scope).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(token => token.SecurityStamp).HasMaxLength(64).IsRequired();
        builder.Property(token => token.UserAgent).HasMaxLength(200).IsRequired();

        // Rotation is a compare-and-set on RevokedAtUtc, so two requests cannot both rotate one token.
        builder.Property(token => token.RevokedAtUtc).IsConcurrencyToken();

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.UserId);
        builder.HasIndex(token => token.FamilyId);
        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(token => token.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TenantMembership>().WithMany()
            .HasForeignKey(token => new { token.TenantId, token.UserId })
            .HasPrincipalKey(membership => new { membership.TenantId, membership.UserId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RefreshToken>().WithMany().HasForeignKey(token => token.ReplacedByTokenId).OnDelete(DeleteBehavior.Restrict);
    }
}
