namespace TailorSpadi.Application.Features.Tenants;

/// <summary>Public fields of a resolved active tenant. Nothing private belongs here.</summary>
public sealed record CurrentTenantResponse(
    string Name,
    string Description,
    string Subdomain,
    string? LogoUrl,
    string PrimaryColor,
    string SecondaryColor,
    string Phone,
    string WhatsApp,
    string Email,
    string Address,
    string City,
    string State,
    string Country);

public sealed record LogoFile(byte[] Content, string ContentType);

public sealed record LogoUrlResponse(string LogoUrl);
