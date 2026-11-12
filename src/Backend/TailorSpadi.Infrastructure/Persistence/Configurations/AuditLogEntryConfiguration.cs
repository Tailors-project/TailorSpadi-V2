using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLog");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Action).HasMaxLength(80).IsRequired();
        builder.Property(entry => entry.Detail).HasMaxLength(200).IsRequired();
        builder.HasIndex(entry => entry.UserId);
        builder.HasIndex(entry => entry.TenantId);
        builder.HasIndex(entry => entry.CreatedAtUtc);
        builder.HasOne<User>().WithMany().HasForeignKey(entry => entry.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(entry => entry.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
