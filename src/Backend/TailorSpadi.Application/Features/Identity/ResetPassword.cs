using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Security;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Features.Identity;

public sealed record ResetPasswordCommand(string Token, string NewPassword, string ConfirmPassword) : ICommand<Unit>;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(command => command.Token).NotEmpty().MaximumLength(256);
        RuleFor(command => command.NewPassword).ValidPassword();
        RuleFor(command => command.ConfirmPassword).Equal(command => command.NewPassword).WithMessage("Passwords do not match.");
    }
}

/// <summary>Consumes a reset token once, replaces the password and revokes every session of the account.</summary>
public sealed class ResetPasswordHandler(
    IAppDbContext db, IPasswordService passwords, ITokenService tokens, SessionService sessions, TimeProvider clock)
    : ICommandHandler<ResetPasswordCommand, Unit>
{
    private static ProblemException InvalidToken() => ProblemException.BadRequest(
        "reset_token_invalid", "This reset link is invalid or has expired. Request a new one.");

    public async ValueTask<Unit> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        DateTime now = clock.GetUtcNow().UtcDateTime;
        string hash = tokens.HashToken(command.Token);
        PasswordResetToken? token = await db.PasswordResetTokens
            .FirstOrDefaultAsync(candidate => candidate.TokenHash == hash, cancellationToken);
        if (token is null || !token.IsUsable(now))
        {
            throw InvalidToken();
        }

        User? user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == token.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw InvalidToken();
        }

        string? policyMessage = PasswordPolicy.Validate(command.NewPassword, user.Email);
        if (policyMessage is not null)
        {
            throw ProblemException.BadRequest("weak_password", policyMessage);
        }

        user.PasswordHash = passwords.Hash(user, command.NewPassword);
        user.RotateSecurityStamp();
        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        token.UsedAtUtc = now;
        await sessions.RevokeSessionsAsync(user.Id, null, cancellationToken);
        db.Audit(clock, "password.reset", user.Id, null);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The token was consumed by a parallel request: single use means this one fails.
            throw InvalidToken();
        }

        return Unit.Value;
    }
}
