using FluentValidation;
using Mediator;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Security;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Features.Identity;

public sealed record AdminLoginCommand(string Email, string Password, string UserAgent) : ICommand<SessionResult>;

public sealed class AdminLoginValidator : AbstractValidator<AdminLoginCommand>
{
    public AdminLoginValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(254);
        RuleFor(command => command.Password).NotEmpty().MaximumLength(PasswordPolicy.MaxLength);
    }
}

/// <summary>Platform administrator sign-in: separate endpoint, audience and account type, platform host only.</summary>
public sealed class AdminLoginHandler(ITenantContext tenantContext, SessionService sessions)
    : ICommandHandler<AdminLoginCommand, SessionResult>
{
    public async ValueTask<SessionResult> Handle(AdminLoginCommand command, CancellationToken cancellationToken)
    {
        if (tenantContext.TenantId is not null)
        {
            throw ProblemException.NotFound("not_found", "Not found.");
        }

        User user = await sessions.AuthenticateAsync(command.Email, command.Password, AuthScope.Admin, cancellationToken);
        return await sessions.StartAsync(user, AuthScope.Admin, null, command.UserAgent, cancellationToken);
    }
}
