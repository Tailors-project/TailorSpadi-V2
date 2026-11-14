namespace TailorSpadi.Domain.Tenancy;

/// <summary>One tailoring business. All businesses share the same API and database.</summary>
public sealed class Tenant
{
    public const int FinalOnboardingStep = 4;

    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Lowercase DNS label. Null until onboarding completes; immutable afterwards.</summary>
    public string? Subdomain { get; set; }
    public string? PreferredSlugBase { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.Onboarding;
    public string Phone { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? LogoStorageKey { get; set; }
    public string PrimaryColor { get; set; } = DefaultPrimaryColor;
    public string SecondaryColor { get; set; } = DefaultSecondaryColor;

    /// <summary>The next onboarding step to show, from 1 to 4.</summary>
    public int OnboardingStep { get; set; } = 1;

    /// <summary>The user who owns the unfinished draft. Cleared when onboarding completes.</summary>
    public Guid? DraftOwnerUserId { get; set; }

    /// <summary>The user who created the tenant. Kept permanently for audit.</summary>
    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Portable optimistic-concurrency token. Replace it on every update.</summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public const string DefaultPrimaryColor = "#1F4E79";
    public const string DefaultSecondaryColor = "#C9A227";

    public bool IsDraft => Status == TenantStatus.Onboarding;

    public void Touch(DateTime nowUtc)
    {
        UpdatedAtUtc = nowUtc;
        ConcurrencyStamp = Guid.NewGuid();
    }

    /// <summary>Moves onboarding forward only; going back to an earlier screen never loses progress.</summary>
    public void AdvanceTo(int step)
    {
        if (step > OnboardingStep)
        {
            OnboardingStep = Math.Min(step, FinalOnboardingStep);
        }
    }

    public void Activate(string subdomain, DateTime nowUtc)
    {
        if (Status != TenantStatus.Onboarding)
        {
            throw new InvalidOperationException("Only a tenant in onboarding can be activated.");
        }

        Subdomain = subdomain;
        Status = TenantStatus.Active;
        OnboardingStep = FinalOnboardingStep;
        DraftOwnerUserId = null;
        Touch(nowUtc);
    }
}
