using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Features.Onboarding;

public sealed record CompleteOnboardingCommand(Guid DraftId, string? SlugBase) : ICommand<CompleteResult>;

public sealed class CompleteOnboardingValidator : AbstractValidator<CompleteOnboardingCommand>
{
    public CompleteOnboardingValidator()
    {
        RuleFor(command => command.DraftId).NotEmpty();
        RuleFor(command => command.SlugBase).Must(value => value is null || value.Length <= 120)
            .WithMessage("That name is too long.");
    }
}

/// <summary>
/// Activates the tenant, allocates its unique subdomain and creates the TenantOwner membership in one
/// SaveChanges. Repeating the call for an already completed draft returns the same result.
/// </summary>
public sealed class CompleteOnboardingHandler(
    IAppDbContext db,
    OnboardingService onboarding,
    SubdomainAllocator allocator,
    IPublicUrlBuilder urls,
    ITenantCache tenantCache,
    TimeProvider clock) : ICommandHandler<CompleteOnboardingCommand, CompleteResult>
{
    private const int MaxAttempts = 5;

    public async ValueTask<CompleteResult> Handle(CompleteOnboardingCommand command, CancellationToken cancellationToken)
    {
        Guid userId = onboarding.RequireOnboardingUser();

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            // The draft id comes from the client, so it is only a lookup key: the creator must be the caller.
            Tenant draft = await db.Tenants.FirstOrDefaultAsync(
                    tenant => tenant.Id == command.DraftId && tenant.CreatedByUserId == userId, cancellationToken)
                ?? throw ProblemException.NotFound("not_found", "That onboarding draft was not found.");

            if (draft.Status == TenantStatus.Active && draft.Subdomain is not null)
            {
                return ToResult(draft);
            }

            if (draft.Status != TenantStatus.Onboarding)
            {
                throw ProblemException.Conflict("not_available", "This business can no longer be set up.");
            }

            EnsureComplete(draft);

            string slugBase = SubdomainAllocator.ResolveBase(command.SlugBase ?? draft.PreferredSlugBase, draft.BusinessName);
            int suffix = await allocator.NextSuffixAsync(slugBase, cancellationToken);
            string subdomain = SubdomainRules.WithSuffix(slugBase, suffix);
            DateTime now = clock.GetUtcNow().UtcDateTime;

            draft.Activate(subdomain, now);
            db.TenantMemberships.Add(new TenantMembership
            {
                Id = Guid.NewGuid(),
                TenantId = draft.Id,
                UserId = userId,
                Role = TenantRole.TenantOwner,
                CreatedAtUtc = now
            });
            db.Audit(clock, "tenant.created", userId, draft.Id, subdomain);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                tenantCache.Invalidate(subdomain);
                return ToResult(draft);
            }
            catch (DbUpdateException exception) when (db.IsUniqueViolation(exception) || exception is DbUpdateConcurrencyException)
            {
                // Someone took the suffix, or this draft was completed in parallel. Start again from stored state.
                db.DiscardChanges();
            }
        }

        throw ProblemException.Conflict("subdomain_busy", "We could not reserve that address. Please try again.");
    }

    private static void EnsureComplete(Tenant draft)
    {
        bool businessComplete = !string.IsNullOrWhiteSpace(draft.BusinessName)
            && !string.IsNullOrWhiteSpace(draft.Phone)
            && !string.IsNullOrWhiteSpace(draft.Email)
            && !string.IsNullOrWhiteSpace(draft.Country);
        if (!businessComplete || draft.OnboardingStep < 3)
        {
            throw ProblemException.Conflict(
                "onboarding_incomplete", "Complete the business information and branding steps first.");
        }
    }

    private CompleteResult ToResult(Tenant tenant)
    {
        string subdomain = tenant.Subdomain!;
        return new CompleteResult(
            tenant.BusinessName, subdomain, urls.Tenant(subdomain, "/"), urls.Tenant(subdomain, "/login"));
    }
}
