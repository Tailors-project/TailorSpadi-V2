using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Security;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Features.Identity;

public sealed record JoinTenantCommand(string Email, string Password, string UserAgent) : ICommand<SessionResult>;

public sealed class JoinTenantValidator : AbstractValidator<JoinTenantCommand>
{
    public JoinTenantValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(254);
        RuleFor(command => command.Password).NotEmpty().MaximumLength(PasswordPolicy.MaxLength);
    }
}

/// <summary>An existing account authenticates, then gains a Customer membership on this store.</summary>
public sealed class JoinTenantHandler(ITenantContext tenantContext, IAppDbContext db, SessionService sessions, TimeProvider clock)
    : ICommandHandler<JoinTenantCommand, SessionResult>
{
    public async ValueTask<SessionResult> Handle(JoinTenantCommand command, CancellationToken cancellationToken)
    {
        if (tenantContext.TenantId is not { } tenantId)
        {
            throw ProblemException.BadRequest("wrong_host", "Joining a store is only available on a store website.");
        }

        User user = await sessions.AuthenticateAsync(command.Email, command.Password, AuthScope.Tenant, cancellationToken);
        TenantMembership? membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == user.Id, cancellationToken);

        if (membership is null)
        {
            membership = new TenantMembership
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = user.Id,
                Role = TenantRole.Customer,
                CreatedAtUtc = clock.GetUtcNow().UtcDateTime
            };
            db.TenantMemberships.Add(membership);
            db.Audit(clock, "customer.joined", user.Id, tenantId);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (db.IsUniqueViolation(exception))
            {
                // A concurrent join created it first; continue with the stored membership.
                db.DiscardChanges();
                membership = await db.TenantMemberships
                    .FirstAsync(m => m.TenantId == tenantId && m.UserId == user.Id, cancellationToken);
            }
        }

        if (!membership.IsActive)
        {
            throw SessionService.InvalidCredentials();
        }

        return await sessions.StartAsync(user, AuthScope.Tenant, membership, command.UserAgent, cancellationToken);
    }
}
