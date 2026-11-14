using Mediator;
using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Security;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Features.Identity;

public sealed record GetMeQuery : IQuery<SessionInfo>;

public sealed class GetMeHandler(
    IAppDbContext db, ICurrentUser currentUser, ITenantContext tenantContext, SessionService sessions)
    : IQueryHandler<GetMeQuery, SessionInfo>
{
    public async ValueTask<SessionInfo> Handle(GetMeQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId || currentUser.Scope is not { } scope)
        {
            throw ProblemException.Unauthorized("invalid_session", "Sign in to continue.");
        }

        User user = await db.Users.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw ProblemException.Unauthorized("invalid_session", "Sign in to continue.");

        TenantMembership? membership = null;
        if (scope == AuthScope.Tenant && tenantContext.TenantId is { } tenantId)
        {
            membership = await db.TenantMemberships.AsNoTracking()
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);
        }

        return await sessions.BuildInfoAsync(user, scope, membership, cancellationToken);
    }
}
