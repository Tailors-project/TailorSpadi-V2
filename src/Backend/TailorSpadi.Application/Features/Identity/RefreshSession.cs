using Mediator;
using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Security;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Features.Identity;

public sealed record RefreshSessionCommand(string? RawToken, AuthScope Scope, string UserAgent) : ICommand<SessionResult>;

/// <summary>
/// Rotates a refresh token. Presenting a token that was already rotated is treated as theft: the whole
/// session family is revoked.
/// </summary>
public sealed class RefreshSessionHandler(
    IAppDbContext db, ITenantContext tenantContext, SessionService sessions, TimeProvider clock, ITokenService tokens)
    : ICommandHandler<RefreshSessionCommand, SessionResult>
{
    private static ProblemException InvalidSession() =>
        ProblemException.Unauthorized("invalid_session", "Your session has expired. Please sign in again.");

    public async ValueTask<SessionResult> Handle(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RawToken))
        {
            throw InvalidSession();
        }

        bool tenantHost = tenantContext.TenantId is not null;
        if ((command.Scope == AuthScope.Tenant) != tenantHost)
        {
            throw InvalidSession();
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        string hash = tokens.HashToken(command.RawToken);
        RefreshToken? current = await db.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);

        if (current is null || current.Scope != command.Scope || current.TenantId != tenantContext.TenantId)
        {
            throw InvalidSession();
        }

        if (current.IsRevoked)
        {
            if (current.ReplacedByTokenId is not null)
            {
                await sessions.RevokeFamilyAsync(current.FamilyId, cancellationToken);
                db.Audit(clock, "session.reuse-detected", current.UserId, current.TenantId);
                await db.SaveChangesAsync(cancellationToken);
            }

            throw InvalidSession();
        }

        if (current.IsExpired(now))
        {
            throw InvalidSession();
        }

        User? user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == current.UserId, cancellationToken);
        if (user is null || !user.IsActive || current.SecurityStamp != user.SecurityStamp
            || (command.Scope == AuthScope.Admin) != user.IsPlatformAdmin)
        {
            throw InvalidSession();
        }

        TenantMembership? membership = null;
        if (command.Scope == AuthScope.Tenant)
        {
            Guid tenantId = tenantContext.TenantId!.Value;
            membership = await db.TenantMemberships
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == user.Id, cancellationToken);
            if (membership is null || !membership.IsActive)
            {
                throw InvalidSession();
            }
        }

        (RefreshToken next, string raw) = sessions.CreateRefreshToken(
            user, command.Scope, current.TenantId, current.FamilyId, command.UserAgent);
        current.RevokedAtUtc = now;
        current.ReplacedByTokenId = next.Id;

        SessionResponse response = await sessions.IssueAccessAsync(user, command.Scope, membership, cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request rotated the same token first: this one is a replay.
            db.DiscardChanges();
            await sessions.RevokeFamilyAsync(current.FamilyId, cancellationToken);
            db.Audit(clock, "session.reuse-detected", current.UserId, current.TenantId);
            await db.SaveChangesAsync(cancellationToken);
            throw InvalidSession();
        }

        return new SessionResult(response, command.Scope, raw, next.ExpiresAtUtc);
    }
}
