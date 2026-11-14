using FluentValidation;
using Mediator;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Security;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Features.Onboarding;

public sealed record SaveBusinessInfoCommand(BusinessInfo Business) : ICommand<OnboardingState>;

public sealed class SaveBusinessInfoValidator : AbstractValidator<SaveBusinessInfoCommand>
{
    public SaveBusinessInfoValidator()
    {
        RuleFor(command => command.Business).NotNull();
        When(command => command.Business is not null, () =>
        {
            RuleFor(command => command.Business.BusinessName).NotEmpty().MinimumLength(2).MaximumLength(100);
            RuleFor(command => command.Business.Description).MaximumLength(1000);
            RuleFor(command => command.Business.Phone).ValidPhone();
            RuleFor(command => command.Business.WhatsApp).Must(PhonePolicy.IsValid)
                .When(command => !string.IsNullOrWhiteSpace(command.Business.WhatsApp))
                .WithMessage("Enter a valid WhatsApp number with 7–15 digits.");
            RuleFor(command => command.Business.Email).ValidEmail();
            RuleFor(command => command.Business.Address).MaximumLength(200);
            RuleFor(command => command.Business.City).MaximumLength(80);
            RuleFor(command => command.Business.State).MaximumLength(80);
            RuleFor(command => command.Business.Country).NotEmpty().MaximumLength(80);
        });
    }
}

/// <summary>Saves step 1. The first save creates the user's single resumable draft.</summary>
public sealed class SaveBusinessInfoHandler(OnboardingService onboarding, IAppDbContext db, TimeProvider clock)
    : ICommandHandler<SaveBusinessInfoCommand, OnboardingState>
{
    public async ValueTask<OnboardingState> Handle(SaveBusinessInfoCommand command, CancellationToken cancellationToken)
    {
        Guid userId = onboarding.RequireOnboardingUser();

        for (int attempt = 0; ; attempt++)
        {
            DateTime now = clock.GetUtcNow().UtcDateTime;
            Tenant? draft = await onboarding.FindDraftAsync(userId, cancellationToken);
            if (draft is null)
            {
                draft = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Status = TenantStatus.Onboarding,
                    DraftOwnerUserId = userId,
                    CreatedByUserId = userId,
                    CreatedAtUtc = now
                };
                db.Tenants.Add(draft);
                db.Audit(clock, "onboarding.started", userId, null);
            }

            BusinessInfo business = command.Business;
            draft.BusinessName = business.BusinessName.Trim();
            draft.Description = business.Description.Trim();
            draft.Phone = business.Phone.Trim();
            draft.WhatsApp = business.WhatsApp.Trim();
            draft.Email = business.Email.Trim();
            draft.Address = business.Address.Trim();
            draft.City = business.City.Trim();
            draft.State = business.State.Trim();
            draft.Country = business.Country.Trim();
            draft.AdvanceTo(2);
            draft.Touch(now);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return await onboarding.BuildStateAsync(draft, cancellationToken);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException exception)
                when (attempt == 0 && (db.IsUniqueViolation(exception)
                    || exception is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException))
            {
                // A parallel request created or changed the draft first; reload it and apply again.
                db.DiscardChanges();
            }
        }
    }
}
