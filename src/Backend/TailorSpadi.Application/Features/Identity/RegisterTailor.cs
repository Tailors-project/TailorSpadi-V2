using FluentValidation;
using Mediator;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Security;

namespace TailorSpadi.Application.Features.Identity;

public sealed record RegisterTailorCommand(
    string FirstName, string LastName, string Email, string Phone, string Password, string ConfirmPassword)
    : ICommand<RegisterResult>;

public sealed class RegisterTailorValidator : AbstractValidator<RegisterTailorCommand>
{
    public RegisterTailorValidator()
    {
        RuleFor(command => command.FirstName).ValidPersonName();
        RuleFor(command => command.LastName).ValidPersonName();
        RuleFor(command => command.Email).ValidEmail();
        RuleFor(command => command.Phone).ValidPhone();
        RuleFor(command => command.Password).ValidPassword();
        RuleFor(command => command.ConfirmPassword).Equal(command => command.Password).WithMessage("Passwords do not match.");
    }
}

public sealed class RegisterTailorHandler(ITenantContext tenantContext, AccountRegistrar registrar)
    : ICommandHandler<RegisterTailorCommand, RegisterResult>
{
    public async ValueTask<RegisterResult> Handle(RegisterTailorCommand command, CancellationToken cancellationToken)
    {
        if (tenantContext.TenantId is not null)
        {
            throw ProblemException.BadRequest("wrong_host", "Tailor registration is only available on the TailorSpadi website.");
        }

        return await registrar.RegisterAsync(
            command.FirstName, command.LastName, command.Email, command.Phone, command.Password, null, cancellationToken);
    }
}
