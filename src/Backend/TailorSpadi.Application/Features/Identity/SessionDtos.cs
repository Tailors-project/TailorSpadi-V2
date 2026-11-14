using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Features.Identity;

public sealed record UserSummary(Guid Id, string Email, string FirstName, string LastName, string? Phone);

public sealed record TenantSummary(string Name, string Subdomain, string WebsiteUrl);

public sealed record BusinessSummary(string Name, string Subdomain, string Role, string WebsiteUrl, string BackofficeUrl);

/// <summary>Everything the React apps need to know about the signed-in session. Contains no secrets.</summary>
public sealed record SessionInfo(
    string Scope,
    UserSummary User,
    string? Role,
    TenantSummary? Tenant,
    IReadOnlyList<BusinessSummary> Businesses,
    bool HasDraft);

public sealed record SessionResponse(string AccessToken, int ExpiresInSeconds, SessionInfo Session);

/// <summary>
/// The result of starting or renewing a session. <see cref="RefreshToken"/> is delivered only in an
/// httpOnly cookie and is null when an existing refresh token stays in place.
/// </summary>
public sealed record SessionResult(
    SessionResponse Response,
    AuthScope Scope,
    string? RefreshToken,
    DateTime? RefreshExpiresAtUtc);
