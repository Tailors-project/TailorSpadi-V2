using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Features.Onboarding;

/// <summary>Draft access rules and state building shared by the onboarding use cases.</summary>
public sealed class OnboardingService(
    IAppDbContext db, ICurrentUser currentUser, ITenantContext tenantContext, IFileStorage storage)
{
    /// <summary>
    /// Onboarding happens on the platform host with a platform session. This is the one place a tenant-owned
    /// record is found without a hostname tenant: through the authenticated user's own draft link.
    /// </summary>
    public Guid RequireOnboardingUser()
    {
        if (currentUser.Scope != AuthScope.Platform || tenantContext.TenantId is not null || currentUser.Id is not { } userId)
        {
            throw ProblemException.Forbidden("forbidden", "You are not allowed to do that.");
        }

        return userId;
    }

    public Task<Tenant?> FindDraftAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Tenants.FirstOrDefaultAsync(
            tenant => tenant.DraftOwnerUserId == userId && tenant.Status == TenantStatus.Onboarding, cancellationToken);

    public async Task<Tenant> RequireDraftAsync(Guid userId, CancellationToken cancellationToken) =>
        await FindDraftAsync(userId, cancellationToken)
        ?? throw ProblemException.Conflict("draft_required", "Save your business information first.");

    public async Task<OnboardingState> BuildStateAsync(Tenant? draft, CancellationToken cancellationToken)
    {
        if (draft is null)
        {
            return new OnboardingState(
                false, null, 1,
                new BusinessInfo(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
                new BrandingInfo(Tenant.DefaultPrimaryColor, Tenant.DefaultSecondaryColor, null));
        }

        string? preview = null;
        if (draft.LogoStorageKey is { } key)
        {
            byte[]? bytes = await storage.ReadAsync(key, cancellationToken);
            if (bytes is not null)
            {
                preview = "data:image/png;base64," + Convert.ToBase64String(bytes);
            }
        }

        return new OnboardingState(
            true,
            draft.Id,
            Math.Clamp(draft.OnboardingStep, 1, Tenant.FinalOnboardingStep),
            new BusinessInfo(
                draft.BusinessName, draft.Description, draft.Phone, draft.WhatsApp, draft.Email,
                draft.Address, draft.City, draft.State, draft.Country),
            new BrandingInfo(draft.PrimaryColor, draft.SecondaryColor, preview), draft.PreferredSlugBase);
    }
}
