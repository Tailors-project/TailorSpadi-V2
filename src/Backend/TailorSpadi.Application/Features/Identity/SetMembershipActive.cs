using Mediator;
using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Features.Identity;

public sealed record SetMembershipActiveCommand(Guid MembershipId, bool IsActive) : ICommand<Unit>;

/// <summary>
/// Activates or deactivates a membership of the current tenant. Only a tenant owner holding
/// ManageBusiness may do this, never on their own membership, and sessions of the member are revoked.
/// </summary>
public sealed class SetMembershipActiveHandler(
    IAppDbContext db, ICurrentUser currentUser, ITenantContext tenantContext, TimeProvider clock)
    : ICommandHandler<SetMembershipActiveCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetMembershipActiveCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Scope != AuthScope.Tenant
            || currentUser.Role is not { } role
            || !Permissions.Has(role, Permissions.ManageBusiness)
            || tenantContext.TenantId is not { } tenantId
            || currentUser.TenantId != tenantId
            || currentUser.Id is not { } actorId)
        {
            throw ProblemException.Forbidden("forbidden", "You are not allowed to do that.");
        }

        TenantMembership membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.Id == command.MembershipId && m.TenantId == tenantId, cancellationToken)
            ?? throw ProblemException.NotFound("not_found", "Member not found.");

        if (membership.UserId == actorId || membership.Role == TenantRole.TenantOwner)
        {
            throw ProblemException.Conflict("not_allowed", "Owners cannot be deactivated here.");
        }

        membership.IsActive = command.IsActive;
        if (!command.IsActive)
        {
            DateTime now = clock.GetUtcNow().UtcDateTime;
            List<RefreshToken> tokens = await db.RefreshTokens
                .Where(token => token.UserId == membership.UserId && token.TenantId == tenantId && token.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);
            foreach (RefreshToken token in tokens)
            {
                token.Revoke(now);
            }
        }

        db.Audit(clock, command.IsActive ? "membership.activated" : "membership.deactivated", actorId, tenantId);
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
