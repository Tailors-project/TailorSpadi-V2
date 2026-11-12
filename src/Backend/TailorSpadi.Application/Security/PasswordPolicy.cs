namespace TailorSpadi.Application.Security;

public static class PasswordPolicy
{
    public const int MinLength = 10;
    public const int MaxLength = 128;

    /// <summary>Returns a user-safe message describing the first violated rule, or null when acceptable.</summary>
    public static string? Validate(string? password, string? email = null)
    {
        if (string.IsNullOrEmpty(password))
        {
            return "Password is required.";
        }

        if (password.Length < MinLength)
        {
            return $"Password must be at least {MinLength} characters.";
        }

        if (password.Length > MaxLength)
        {
            return $"Password must be at most {MaxLength} characters.";
        }

        if (CommonPasswords.Contains(password) || password.Distinct().Count() < 5)
        {
            return "This password is too common or too simple. Choose a less predictable one.";
        }

        if (!string.IsNullOrWhiteSpace(email) && string.Equals(password, email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return "Password must be different from your email address.";
        }

        return null;
    }
}
