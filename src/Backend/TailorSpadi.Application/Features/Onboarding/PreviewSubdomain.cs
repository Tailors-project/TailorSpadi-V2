using FluentValidation;
using Mediator;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Features.Onboarding;

/// <param name="SlugBase">Optional preferred name; when omitted the draft's business name is used.</param>
public sealed record PreviewSubdomainQuery(string? SlugBase) : IQuery<SubdomainPreview>;

public sealed class PreviewSubdomainValidator : AbstractValidator<PreviewSubdomainQuery>
{
    public PreviewSubdomainValidator()
    {
        RuleFor(query => query.SlugBase).Must(value => value is null || value.Length <= 120)
            .WithMessage("That name is too long.");
    }
}

public sealed class PreviewSubdomainHandler(OnboardingService onboarding, SubdomainAllocator allocator, IPublicUrlBuilder urls)
    : IQueryHandler<PreviewSubdomainQuery, SubdomainPreview>
{
    public async ValueTask<SubdomainPreview> Handle(PreviewSubdomainQuery query, CancellationToken cancellationToken)
    {
        Guid userId = onboarding.RequireOnboardingUser();
        Tenant draft = await onboarding.RequireDraftAsync(userId, cancellationToken);

        string slugBase = SubdomainAllocator.ResolveBase(null, draft.BusinessName);
        await allocator.EnsureAvailableAsync(slugBase, draft.Id, cancellationToken);
        string suggested = slugBase;
        return new SubdomainPreview(slugBase, suggested, urls.Tenant(suggested, "/"), true);
    }
}
