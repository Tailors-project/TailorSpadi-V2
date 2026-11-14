using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Options;
using TailorSpadi.Application.Security;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Infrastructure.Security;

namespace TailorSpadi.Api.Security;

public static class AuthenticationSetup
{
    public static IServiceCollection AddTailorSpadiAuthentication(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<CookieRequestGuard>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthOptions>>((jwt, authOptions) =>
            {
                AuthOptions auth = authOptions.Value;
                jwt.MapInboundClaims = false;
                jwt.RequireHttpsMetadata = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = auth.Issuer,
                    ValidateAudience = true,
                    ValidAudiences = [auth.TenantAudience, auth.PlatformAudience, auth.AdminAudience],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtKeys.Create(auth),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(auth.ClockSkewSeconds),
                    NameClaimType = "sub",
                    RoleClaimType = TokenService.RoleClaim
                };
                jwt.Events = new JwtBearerEvents { OnTokenValidated = ValidateSessionAsync };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.AnySession, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(AuthPolicies.TenantUser, policy => policy
                .RequireAuthenticatedUser().RequireClaim(TokenService.ScopeClaim, "tenant"))
            .AddPolicy(AuthPolicies.TenantBackoffice, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(TokenService.ScopeClaim, "tenant")
                .RequireClaim(TokenService.RoleClaim, nameof(TenantRole.TenantOwner), nameof(TenantRole.TenantManager), nameof(TenantRole.TenantStaff)))
            .AddPolicy(AuthPolicies.ManageBusiness, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(TokenService.ScopeClaim, "tenant")
                .RequireAssertion(context =>
                    Enum.TryParse(context.User.FindFirst(TokenService.RoleClaim)?.Value, out TenantRole role)
                    && Permissions.Has(role, Permissions.ManageBusiness)))
            .AddPolicy(AuthPolicies.PlatformUser, policy => policy
                .RequireAuthenticatedUser().RequireClaim(TokenService.ScopeClaim, "platform"))
            .AddPolicy(AuthPolicies.PlatformAdmin, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(TokenService.ScopeClaim, "admin")
                .RequireClaim(TokenService.RoleClaim, "PlatformAdmin"));

        return services;
    }

    /// <summary>
    /// After the signature, issuer, audience and lifetime pass, check the token still describes a live session:
    /// audience matches scope, the host matches the tenant, the account and membership are active and the
    /// security stamp is current.
    /// </summary>
    private static async Task ValidateSessionAsync(TokenValidatedContext context)
    {
        IServiceProvider services = context.HttpContext.RequestServices;
        AuthOptions auth = services.GetRequiredService<IOptions<AuthOptions>>().Value;
        ClaimsPrincipal principal = context.Principal!;

        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out Guid userId)
            || !Enum.TryParse(principal.FindFirst(TokenService.ScopeClaim)?.Value, ignoreCase: true, out AuthScope scope)
            || principal.FindFirst(TokenService.StampClaim)?.Value is not { Length: > 0 } stamp)
        {
            context.Fail("Malformed session.");
            return;
        }

        string expectedAudience = JwtKeys.AudienceFor(auth, scope);
        if (context.SecurityToken is not JsonWebToken token || !token.Audiences.Contains(expectedAudience))
        {
            context.Fail("Wrong audience for scope.");
            return;
        }

        Guid? tenantId = Guid.TryParse(principal.FindFirst(TokenService.TenantClaim)?.Value, out Guid parsedTenant) ? parsedTenant : null;
        var claims = new SessionClaims(
            userId,
            scope,
            tenantId,
            principal.FindFirst(TokenService.RoleClaim)?.Value,
            stamp);

        string? failure = await services.GetRequiredService<SessionValidator>()
            .ValidateAsync(claims, context.HttpContext.RequestAborted);
        if (failure is not null)
        {
            context.Fail(failure);
        }
    }
}
