namespace TailorSpadi.Application.Features.Onboarding;

public sealed record BusinessInfo(
    string BusinessName,
    string Description,
    string Phone,
    string WhatsApp,
    string Email,
    string Address,
    string City,
    string State,
    string Country);

public sealed record BrandingInfo(string PrimaryColor, string SecondaryColor, string? LogoPreviewDataUrl);

public sealed record OnboardingState(bool HasDraft, Guid? DraftId, int Step, BusinessInfo Business, BrandingInfo Branding, string? SlugBase = null);

public sealed record SubdomainPreview(string SlugBase, string SuggestedSubdomain, string Url, bool Available);

public sealed record CompleteResult(string BusinessName, string Subdomain, string WebsiteUrl, string BackofficeUrl);
