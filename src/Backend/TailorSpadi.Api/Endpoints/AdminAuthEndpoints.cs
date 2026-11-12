using Mediator;
using Microsoft.Extensions.Options;
using TailorSpadi.Api.Contracts;
using TailorSpadi.Api.Security;
using TailorSpadi.Application.Features.Identity;
using TailorSpadi.Application.Options;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Api.Endpoints;

/// <summary>Platform administrator sign-in: its own endpoints, audience, cookie and account type.</summary>
public static class AdminAuthEndpoints
{
    public static IEndpointRouteBuilder MapAdminAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/admin/auth").WithTags("Admin");

        group.MapPost("/login", async (
                LoginRequest request, HttpContext http, IMediator mediator, IOptions<AuthOptions> options, CancellationToken ct) =>
            {
                SessionResult result = await mediator.Send(
                    new AdminLoginCommand(request.Email ?? string.Empty, request.Password ?? string.Empty, SessionHttp.UserAgent(http)), ct);
                return SessionHttp.Respond(http, options.Value, result);
            })
            .RequireRateLimiting(RateLimitPolicies.Login);

        group.MapPost("/refresh", (HttpContext http, IMediator mediator, IOptions<AuthOptions> options, CancellationToken ct) =>
                AuthEndpoints.RefreshAsync(http, AuthScope.Admin, mediator, options.Value, ct))
            .AddEndpointFilter<CookieRequestGuard>()
            .RequireRateLimiting(RateLimitPolicies.Refresh);

        group.MapPost("/logout", (HttpContext http, IMediator mediator, IOptions<AuthOptions> options, CancellationToken ct) =>
                AuthEndpoints.LogoutAsync(http, AuthScope.Admin, mediator, options.Value, ct))
            .AddEndpointFilter<CookieRequestGuard>()
            .RequireRateLimiting(RateLimitPolicies.Refresh);

        // A protected probe the admin shell uses to confirm an admin session; tenant and platform tokens are refused.
        group.MapGet("/me", async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetMeQuery(), ct)))
            .RequireAuthorization(AuthPolicies.PlatformAdmin);

        return endpoints;
    }
}
