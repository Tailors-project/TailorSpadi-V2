using FluentValidation;
using Mediator;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Security;

namespace TailorSpadi.Application.Features.Identity;

public sealed record RegisterCustomerCommand(
    string FirstName, string LastName, string Email, string Phone, string Password, string ConfirmPassword)
    : ICommand<RegisterResult>;

public sealed class RegisterCustomerValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerValidator()
    {
        RuleFor(command => command.FirstName).ValidPersonName();
        RuleFor(command => command.LastName).ValidPersonName();
        RuleFor(command => command.Email).ValidEmail();
        RuleFor(command => command.Phone).Matches(@"^[0-9+()\-\s]{7,20}$").When(command => !string.IsNullOrWhiteSpace(command.Phone))
            .WithMessage("Enter a valid phone number.");
        RuleFor(command => command.Password).ValidPassword();
        RuleFor(command => command.ConfirmPassword).Equal(command => command.Password).WithMessage("Passwords do not match.");
    }
}

/// <summary>Registers a customer on the resolved store. An existing account must sign in and join instead.</summary>
public sealed class RegisterCustomerHandler(ITenantContext tenantContext, AccountRegistrar registrar)
    : ICommandHandler<RegisterCustomerCommand, RegisterResult>
{
    public async ValueTask<RegisterResult> Handle(RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        if (tenantContext.TenantId is not { } tenantId)
        {
            throw ProblemException.BadRequest("wrong_host", "Customer registration is only available on a store website.");
        }

        return await registrar.RegisterAsync(
            command.FirstName, command.LastName, command.Email, command.Phone, command.Password, tenantId, cancellationToken);
    }
}
