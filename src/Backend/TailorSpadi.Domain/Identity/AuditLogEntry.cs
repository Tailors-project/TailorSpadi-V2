namespace TailorSpadi.Domain.Identity;

/// <summary>An audit record. Never store passwords, tokens, hashes or request bodies here.</summary>
public sealed class AuditLogEntry
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;

    /// <summary>Short, non-secret context such as a role name or an outcome.</summary>
    public string Detail { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
