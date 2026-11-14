using Mediator;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Features.Onboarding;

public sealed record GetOnboardingQuery : IQuery<OnboardingState>;

public sealed class GetOnboardingHandler(OnboardingService onboarding) : IQueryHandler<GetOnboardingQuery, OnboardingState>
{
    public async ValueTask<OnboardingState> Handle(GetOnboardingQuery query, CancellationToken cancellationToken)
    {
        Guid userId = onboarding.RequireOnboardingUser();
        Tenant? draft = await onboarding.FindDraftAsync(userId, cancellationToken);
        return await onboarding.BuildStateAsync(draft, cancellationToken);
    }
}
