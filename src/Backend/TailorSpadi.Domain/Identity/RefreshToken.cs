namespace TailorSpadi.Domain.Identity;

/// <summary>A hashed, rotating, single-use refresh token. Tokens of one login share a <see cref="FamilyId"/>.</summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>The tenant a tenant-scope session is bound to; null for platform and admin sessions.</summary>
    public Guid? TenantId { get; set; }

    public AuthScope Scope { get; set; }
    public Guid FamilyId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string SecurityStamp { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string UserAgent { get; set; } = string.Empty;

    public bool IsRevoked => RevokedAtUtc is not null;

    public bool IsExpired(DateTime nowUtc) => ExpiresAtUtc <= nowUtc;

    public bool IsUsable(DateTime nowUtc) => !IsRevoked && !IsExpired(nowUtc);

    public void Revoke(DateTime nowUtc)
    {
        RevokedAtUtc ??= nowUtc;
    }
}
