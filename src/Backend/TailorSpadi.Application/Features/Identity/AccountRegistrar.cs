using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Features.Identity;

public sealed record RegisterResult(Guid UserId);

/// <summary>Creates a global user, optionally with a Customer membership on the current tenant.</summary>
public sealed class AccountRegistrar(IAppDbContext db, IPasswordService passwords, TimeProvider clock)
{
    public async Task<RegisterResult> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string phone,
        string password,
        Guid? customerTenantId,
        CancellationToken cancellationToken)
    {
        string normalized = User.NormalizeEmail(email);
        bool exists = await db.Users.AnyAsync(user => user.NormalizedEmail == normalized, cancellationToken);
        if (exists)
        {
            throw EmailTaken();
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            NormalizedEmail = normalized,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Phone = phone.Trim(),
            CreatedAtUtc = now
        };
        user.PasswordHash = passwords.Hash(user, password);
        db.Users.Add(user);

        if (customerTenantId is { } tenantId)
        {
            db.TenantMemberships.Add(new TenantMembership
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = user.Id,
                Role = TenantRole.Customer,
                CreatedAtUtc = now
            });
        }

        db.Audit(clock, customerTenantId is null ? "user.registered" : "customer.registered", user.Id, customerTenantId);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (db.IsUniqueViolation(exception))
        {
            throw EmailTaken();
        }

        return new RegisterResult(user.Id);
    }

    private static ProblemException EmailTaken() => ProblemException.Conflict(
        "email_taken", "An account with this email already exists. Sign in or reset your password instead.");
}
