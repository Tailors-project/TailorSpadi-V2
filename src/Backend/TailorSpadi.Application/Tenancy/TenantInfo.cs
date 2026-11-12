namespace TailorSpadi.Application.Tenancy;

/// <summary>Public-safe facts about a resolved active tenant.</summary>
public sealed record TenantInfo(
    Guid Id,
    string Name,
    string Description,
    string Subdomain,
    string? LogoStorageKey,
    string PrimaryColor,
    string SecondaryColor,
    string Phone,
    string WhatsApp,
    string Email,
    string Address,
    string City,
    string State,
    string Country);
