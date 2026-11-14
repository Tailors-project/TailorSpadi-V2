using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Abstractions;

public sealed record AccessTokenRequest(Guid UserId, AuthScope Scope, Guid? TenantId, string? Role, string SecurityStamp);

public sealed record AccessToken(string Token, int ExpiresInSeconds);

public interface ITokenService
{
    AccessToken CreateAccessToken(AccessTokenRequest request);

    /// <summary>A URL-safe random token of at least 256 bits.</summary>
    string GenerateOpaqueToken();

    /// <summary>One-way hash used to store refresh and reset tokens.</summary>
    string HashToken(string token);
}
