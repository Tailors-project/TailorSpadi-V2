using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Options;

namespace TailorSpadi.Infrastructure.Security;

/// <summary>Creates short-lived signed access tokens and high-entropy opaque tokens with one-way hashes.</summary>
public sealed class TokenService : ITokenService
{
    public const string ScopeClaim = "tsc";
    public const string TenantClaim = "tid";
    public const string StampClaim = "sv";
    public const string RoleClaim = "role";

    private readonly AuthOptions _options;
    private readonly TimeProvider _clock;
    private readonly SymmetricSecurityKey _key;
    private readonly JsonWebTokenHandler _handler = new();

    public TokenService(IOptions<AuthOptions> options, TimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
        _key = JwtKeys.Create(_options);
    }

    public AccessToken CreateAccessToken(AccessTokenRequest request)
    {
        DateTime now = _clock.GetUtcNow().UtcDateTime;
        DateTime expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = request.UserId.ToString("D"),
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            [ScopeClaim] = request.Scope.ToString().ToLowerInvariant(),
            [StampClaim] = request.SecurityStamp
        };
        if (request.TenantId is { } tenantId)
        {
            claims[TenantClaim] = tenantId.ToString("D");
        }

        if (request.Role is { } role)
        {
            claims[RoleClaim] = role;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = JwtKeys.AudienceFor(_options, request.Scope),
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Claims = claims,
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(_handler.CreateToken(descriptor), (int)(expires - now).TotalSeconds);
    }

    public string GenerateOpaqueToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public string HashToken(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
