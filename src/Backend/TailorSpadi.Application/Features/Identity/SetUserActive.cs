using Mediator;
using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Security;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Features.Identity;

public sealed record SetUserActiveCommand(Guid UserId, bool IsActive) : ICommand<Unit>;

/// <summary>Platform administrators activate or deactivate accounts. Deactivation revokes all sessions.</summary>
public sealed class SetUserActiveHandler(
    IAppDbContext db, ICurrentUser currentUser, SessionService sessions, TimeProvider clock)
    : ICommandHandler<SetUserActiveCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetUserActiveCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Scope != AuthScope.Admin || currentUser.Id is not { } actorId)
        {
            throw ProblemException.Forbidden("forbidden", "You are not allowed to do that.");
        }

        User user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == command.UserId, cancellationToken)
            ?? throw ProblemException.NotFound("not_found", "User not found.");

        if (user.Id == actorId)
        {
            throw ProblemException.Conflict("not_allowed", "You cannot change your own account status.");
        }

        user.IsActive = command.IsActive;
        if (!command.IsActive)
        {
            user.RotateSecurityStamp();
            await sessions.RevokeSessionsAsync(user.Id, null, cancellationToken);
        }

        db.Audit(clock, command.IsActive ? "user.activated" : "user.deactivated", actorId, null);
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
