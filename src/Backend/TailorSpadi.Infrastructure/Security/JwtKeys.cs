using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TailorSpadi.Application.Options;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Infrastructure.Security;

/// <summary>Builds the symmetric signing key shared by token creation and token validation.</summary>
public static class JwtKeys
{
    public static SymmetricSecurityKey Create(AuthOptions options)
    {
        if (!AuthOptions.IsAcceptableSigningKey(options.SigningKey))
        {
            throw new InvalidOperationException(
                "Auth:SigningKey is missing or too weak. Configure a random secret of at least 32 characters.");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
    }

    public static string AudienceFor(AuthOptions options, AuthScope scope) => scope switch
    {
        AuthScope.Tenant => options.TenantAudience,
        AuthScope.Platform => options.PlatformAudience,
        _ => options.AdminAudience
    };
}
