using TailorSpadi.Application.Abstractions;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Common;

public static class AuditWriter
{
    /// <summary>Adds an audit entry to the current unit of work. Never pass secrets in <paramref name="detail"/>.</summary>
    public static void Audit(
        this IAppDbContext db, TimeProvider clock, string action, Guid? userId, Guid? tenantId, string detail = "")
    {
        db.AuditLog.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            Action = action,
            UserId = userId,
            TenantId = tenantId,
            Detail = detail.Length > 200 ? detail[..200] : detail,
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime
        });
    }
}
