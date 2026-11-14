using Mediator;
using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Features.Tenants;

public sealed record SetTenantStatusCommand(Guid TenantId, TenantStatus Status) : ICommand<Unit>;

public sealed class SetTenantStatusHandler(
    IAppDbContext db, ICurrentUser currentUser, ITenantCache cache, TimeProvider clock)
    : ICommandHandler<SetTenantStatusCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetTenantStatusCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Scope != AuthScope.Admin || currentUser.Id is not { } actorId)
        {
            throw ProblemException.Forbidden("forbidden", "You are not allowed to change a business status.");
        }

        if (command.Status is not (TenantStatus.Active or TenantStatus.Suspended or TenantStatus.Deactivated))
        {
            throw ProblemException.BadRequest("invalid_status", "Choose an active, suspended or deactivated status.");
        }

        Tenant tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == command.TenantId, cancellationToken)
            ?? throw ProblemException.NotFound("not_found", "Business not found.");
        if (tenant.IsDraft || tenant.Subdomain is null)
        {
            throw ProblemException.Conflict("onboarding_incomplete", "Complete onboarding before changing the business status.");
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        tenant.Status = command.Status;
        tenant.Touch(now);
        if (command.Status != TenantStatus.Active)
        {
            List<RefreshToken> tokens = await db.RefreshTokens
                .Where(token => token.TenantId == tenant.Id && token.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);
            foreach (RefreshToken token in tokens)
            {
                token.Revoke(now);
            }
        }

        db.Audit(clock, "tenant.status-changed", actorId, tenant.Id, command.Status.ToString());
        await db.SaveChangesAsync(cancellationToken);
        cache.Invalidate(tenant.Subdomain);
        return Unit.Value;
    }
}
