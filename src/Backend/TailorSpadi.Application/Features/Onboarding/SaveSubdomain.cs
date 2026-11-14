using FluentValidation;
using Mediator;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Features.Onboarding;

public sealed record SaveSubdomainCommand(string? SlugBase) : ICommand<OnboardingState>;

public sealed class SaveSubdomainValidator : AbstractValidator<SaveSubdomainCommand>
{
    public SaveSubdomainValidator()
    {
        RuleFor(command => command.SlugBase).MaximumLength(120);
    }
}

public sealed class SaveSubdomainHandler(
    IAppDbContext db, OnboardingService onboarding, SubdomainAllocator allocator, TimeProvider clock)
    : ICommandHandler<SaveSubdomainCommand, OnboardingState>
{
    public async ValueTask<OnboardingState> Handle(SaveSubdomainCommand command, CancellationToken cancellationToken)
    {
        Guid userId = onboarding.RequireOnboardingUser();
        Tenant draft = await onboarding.RequireDraftAsync(userId, cancellationToken);
        if (draft.OnboardingStep < 3)
        {
            throw ProblemException.Conflict("onboarding_incomplete", "Save your business details and branding first.");
        }

        string slugBase = SubdomainAllocator.ResolveBase(null, draft.BusinessName);
        await allocator.EnsureAvailableAsync(slugBase, draft.Id, cancellationToken);
        draft.PreferredSlugBase = slugBase;
        draft.AdvanceTo(4);
        draft.Touch(clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        return await onboarding.BuildStateAsync(draft, cancellationToken);
    }
}
