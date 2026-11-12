namespace TailorSpadi.Domain.Identity;

/// <summary>A hashed, single-use, short-lived password reset token.</summary>
public sealed class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }

    public bool IsUsable(DateTime nowUtc) => UsedAtUtc is null && ExpiresAtUtc > nowUtc;
}
