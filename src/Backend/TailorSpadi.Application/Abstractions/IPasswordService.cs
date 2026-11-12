using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Abstractions;

public enum PasswordVerification
{
    Failed = 0,
    Success = 1,
    SuccessRehashNeeded = 2
}

public interface IPasswordService
{
    string Hash(User user, string password);
    PasswordVerification Verify(User user, string passwordHash, string password);

    /// <summary>Spends comparable time to a real verification so unknown accounts are not distinguishable by timing.</summary>
    void BurnTime(string password);
}
