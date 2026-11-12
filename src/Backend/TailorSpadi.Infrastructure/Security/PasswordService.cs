using Microsoft.AspNetCore.Identity;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Infrastructure.Security;

/// <summary>PBKDF2 password hashing through ASP.NET Core's versioned PasswordHasher (no Identity stores or UI).</summary>
public sealed class PasswordService : IPasswordService
{
    // OWASP guidance for PBKDF2-HMAC-SHA512. Hashes made with fewer iterations are upgraded on next login.
    private const int IterationCount = 210_000;

    private readonly PasswordHasher<User> _hasher = new(
        Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions { IterationCount = IterationCount }));

    private readonly string _dummyHash;

    public PasswordService()
    {
        _dummyHash = _hasher.HashPassword(new User(), Guid.NewGuid().ToString("N"));
    }

    public string Hash(User user, string password) => _hasher.HashPassword(user, password);

    public PasswordVerification Verify(User user, string passwordHash, string password)
    {
        return _hasher.VerifyHashedPassword(user, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SuccessRehashNeeded,
            _ => PasswordVerification.Failed
        };
    }

    public void BurnTime(string password)
    {
        _ = _hasher.VerifyHashedPassword(new User(), _dummyHash, password);
    }
}
