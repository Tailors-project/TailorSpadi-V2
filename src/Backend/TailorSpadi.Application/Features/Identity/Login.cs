using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Security;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Features.Identity;

public sealed record LoginCommand(string Email, string Password, string UserAgent) : ICommand<SessionResult>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(254);
        RuleFor(command => command.Password).NotEmpty().MaximumLength(PasswordPolicy.MaxLength);
    }
}

/// <summary>
/// Signs in on the current host. A tenant host yields a session bound to that tenant; a platform host
/// yields a platform session for onboarding. Sessions never cross hosts.
/// </summary>
public sealed class LoginHandler(ITenantContext tenantContext, IAppDbContext db, SessionService sessions, TimeProvider clock)
    : ICommandHandler<LoginCommand, SessionResult>
{
    public async ValueTask<SessionResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        AuthScope scope = tenantContext.TenantId is null ? AuthScope.Platform : AuthScope.Tenant;
        User user = await sessions.AuthenticateAsync(command.Email, command.Password, scope, cancellationToken);

        TenantMembership? membership = null;
        if (scope == AuthScope.Tenant)
        {
            Guid tenantId = tenantContext.TenantId!.Value;
            membership = await db.TenantMemberships
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == user.Id, cancellationToken);
            if (membership is null)
            {
                db.Audit(clock, "login.rejected", user.Id, tenantId, "membership-required");
                await db.SaveChangesAsync(cancellationToken);
                throw ProblemException.Forbidden(
                    "membership_required", "This account is not registered with this store yet.");
            }

            if (!membership.IsActive)
            {
                db.Audit(clock, "login.rejected", user.Id, tenantId, "membership-inactive");
                await db.SaveChangesAsync(cancellationToken);
                throw SessionService.InvalidCredentials();
            }
        }

        return await sessions.StartAsync(user, scope, membership, command.UserAgent, cancellationToken);
    }
}
