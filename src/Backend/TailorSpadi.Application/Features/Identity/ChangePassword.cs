using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Security;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Features.Identity;

/// <param name="CurrentRefreshToken">The caller's refresh cookie, so that session survives while all others are revoked.</param>
public sealed record ChangePasswordCommand(
    string CurrentPassword, string NewPassword, string ConfirmPassword, string? CurrentRefreshToken)
    : ICommand<SessionResult>;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(command => command.CurrentPassword).NotEmpty().MaximumLength(PasswordPolicy.MaxLength);
        RuleFor(command => command.NewPassword).ValidPassword();
        RuleFor(command => command.ConfirmPassword).Equal(command => command.NewPassword).WithMessage("Passwords do not match.");
    }
}

public sealed class ChangePasswordHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IPasswordService passwords,
    ITokenService tokens,
    SessionService sessions,
    TimeProvider clock) : ICommandHandler<ChangePasswordCommand, SessionResult>
{
    public async ValueTask<SessionResult> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId || currentUser.Scope is not { } scope)
        {
            throw ProblemException.Unauthorized("invalid_session", "Sign in to continue.");
        }

        User user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw ProblemException.Unauthorized("invalid_session", "Sign in to continue.");

        if (passwords.Verify(user, user.PasswordHash, command.CurrentPassword) == PasswordVerification.Failed)
        {
            db.Audit(clock, "password.change-failed", user.Id, tenantContext.TenantId);
            await db.SaveChangesAsync(cancellationToken);
            throw ProblemException.BadRequest("wrong_password", "The current password is incorrect.");
        }

        string? policyMessage = PasswordPolicy.Validate(command.NewPassword, user.Email);
        if (policyMessage is not null)
        {
            throw ProblemException.BadRequest("weak_password", policyMessage);
        }

        Guid? keepFamily = null;
        if (!string.IsNullOrWhiteSpace(command.CurrentRefreshToken))
        {
            string hash = tokens.HashToken(command.CurrentRefreshToken);
            keepFamily = await db.RefreshTokens
                .Where(token => token.TokenHash == hash && token.UserId == user.Id && token.RevokedAtUtc == null)
                .Select(token => (Guid?)token.FamilyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        user.PasswordHash = passwords.Hash(user, command.NewPassword);
        user.RotateSecurityStamp();
        await sessions.RevokeSessionsAsync(user.Id, keepFamily, cancellationToken);
        if (keepFamily is { } familyId)
        {
            List<RefreshToken> retainedTokens = await db.RefreshTokens
                .Where(token => token.UserId == user.Id && token.FamilyId == familyId && token.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);
            foreach (RefreshToken token in retainedTokens)
            {
                token.SecurityStamp = user.SecurityStamp;
            }
        }
        db.Audit(clock, "password.changed", user.Id, tenantContext.TenantId);
        await db.SaveChangesAsync(cancellationToken);

        TenantMembership? membership = null;
        if (scope == AuthScope.Tenant && tenantContext.TenantId is { } tenantId)
        {
            membership = await db.TenantMemberships
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == user.Id, cancellationToken);
        }

        // The stamp changed, so the caller's old access token is now invalid; hand back a fresh one.
        SessionResponse response = await sessions.IssueAccessAsync(user, scope, membership, cancellationToken);
        return new SessionResult(response, scope, null, null);
    }
}
