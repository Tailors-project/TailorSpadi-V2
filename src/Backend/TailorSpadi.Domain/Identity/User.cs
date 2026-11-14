namespace TailorSpadi.Domain.Identity;

/// <summary>A global identity. Tenant access is expressed by <see cref="TenantMembership"/> rows.</summary>
public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;

    /// <summary>Trimmed, upper-invariant email used for uniqueness and lookup.</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Rotated on password change/reset and deactivation; carried in access tokens to revoke them.</summary>
    public string SecurityStamp { get; set; } = NewSecurityStamp();

    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; }
    public bool IsPlatformAdmin { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    public static string NewSecurityStamp() => Guid.NewGuid().ToString("N");

    public bool IsLockedOut(DateTime nowUtc) => LockoutEndUtc is { } end && end > nowUtc;

    public void RotateSecurityStamp() => SecurityStamp = NewSecurityStamp();

    public void RegisterFailedLogin(DateTime nowUtc, int maxAttempts, TimeSpan lockoutDuration)
    {
        if (LockoutEndUtc is { } end && end <= nowUtc)
        {
            LockoutEndUtc = null;
            FailedLoginCount = 0;
        }

        FailedLoginCount++;
        if (FailedLoginCount >= maxAttempts)
        {
            LockoutEndUtc = nowUtc + lockoutDuration;
            FailedLoginCount = 0;
        }
    }

    public void RegisterSuccessfulLogin(DateTime nowUtc)
    {
        FailedLoginCount = 0;
        LockoutEndUtc = null;
        LastLoginAtUtc = nowUtc;
    }
}
