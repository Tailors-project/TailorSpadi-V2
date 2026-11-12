namespace TailorSpadi.Application.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "tailorspadi";
    public string TenantAudience { get; set; } = "tailorspadi-tenant";
    public string PlatformAudience { get; set; } = "tailorspadi-platform";
    public string AdminAudience { get; set; } = "tailorspadi-admin";

    /// <summary>HMAC signing secret. Comes from user secrets or the environment, never from source control.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
    public int ClockSkewSeconds { get; set; } = 30;
    public int MaxFailedLogins { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int ResetTokenMinutes { get; set; } = 30;

    /// <summary>Sets the Secure attribute of refresh cookies. Keep true except for plain-HTTP local experiments.</summary>
    public bool CookieSecure { get; set; } = true;

    /// <summary>Extra exact browser origins (for example Vite dev servers) accepted by the CSRF origin check.</summary>
    public string[] AllowedOrigins { get; set; } = [];

    public AdminSeedOptions AdminSeed { get; set; } = new();

    /// <summary>Rejects short, low-variety or obviously placeholder secrets.</summary>
    public static bool IsAcceptableSigningKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32 || key.Distinct().Count() < 12)
        {
            return false;
        }

        string[] placeholders = ["replace", "changeme", "change-me", "password", "secret", "example", "placeholder"];
        return !placeholders.Any(word => key.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class AdminSeedOptions
{
    /// <summary>Must be set explicitly. There are no default credentials.</summary>
    public bool Enabled { get; set; }

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = "Platform";
    public string LastName { get; set; } = "Administrator";
}
