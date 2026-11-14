using FluentValidation;
using Mediator;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Security;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Features.Onboarding;

public sealed record SaveBrandingCommand(string PrimaryColor, string SecondaryColor) : ICommand<OnboardingState>;

public sealed class SaveBrandingValidator : AbstractValidator<SaveBrandingCommand>
{
    public SaveBrandingValidator()
    {
        RuleFor(command => command.PrimaryColor).ValidHexColor();
        RuleFor(command => command.SecondaryColor).ValidHexColor();
    }
}

public sealed class SaveBrandingHandler(OnboardingService onboarding, IAppDbContext db, TimeProvider clock)
    : ICommandHandler<SaveBrandingCommand, OnboardingState>
{
    public async ValueTask<OnboardingState> Handle(SaveBrandingCommand command, CancellationToken cancellationToken)
    {
        Guid userId = onboarding.RequireOnboardingUser();
        Tenant draft = await onboarding.RequireDraftAsync(userId, cancellationToken);

        draft.PrimaryColor = command.PrimaryColor.ToUpperInvariant();
        draft.SecondaryColor = command.SecondaryColor.ToUpperInvariant();
        draft.AdvanceTo(3);
        draft.Touch(clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        return await onboarding.BuildStateAsync(draft, cancellationToken);
    }
}
