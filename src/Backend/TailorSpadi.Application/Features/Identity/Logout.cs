using Mediator;
using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Security;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Features.Identity;

public sealed record LogoutCommand(string? RawToken) : ICommand<Unit>;

/// <summary>Revokes the session family of the presented refresh token. Unknown tokens are ignored.</summary>
public sealed class LogoutHandler(IAppDbContext db, SessionService sessions, ITokenService tokens, TimeProvider clock)
    : ICommandHandler<LogoutCommand, Unit>
{
    public async ValueTask<Unit> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RawToken))
        {
            return Unit.Value;
        }

        string hash = tokens.HashToken(command.RawToken);
        RefreshToken? token = await db.RefreshTokens.FirstOrDefaultAsync(candidate => candidate.TokenHash == hash, cancellationToken);
        if (token is null)
        {
            return Unit.Value;
        }

        await sessions.RevokeFamilyAsync(token.FamilyId, cancellationToken);
        db.Audit(clock, "logout", token.UserId, token.TenantId);
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
