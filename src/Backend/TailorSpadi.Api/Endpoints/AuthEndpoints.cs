using Mediator;
using Microsoft.Extensions.Options;
using TailorSpadi.Api.Contracts;
using TailorSpadi.Api.Errors;
using TailorSpadi.Api.Security;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Features.Identity;
using TailorSpadi.Application.Options;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register-tailor", async (RegisterRequest request, IMediator mediator, CancellationToken ct) =>
                Results.Json(await mediator.Send(request.ToTailorCommand(), ct), statusCode: StatusCodes.Status201Created))
            .RequireRateLimiting(RateLimitPolicies.Register);

        group.MapPost("/register-customer", async (RegisterRequest request, IMediator mediator, CancellationToken ct) =>
                Results.Json(await mediator.Send(request.ToCustomerCommand(), ct), statusCode: StatusCodes.Status201Created))
            .RequireRateLimiting(RateLimitPolicies.Register);

        group.MapPost("/login", async (
                LoginRequest request, HttpContext http, IMediator mediator, IOptions<AuthOptions> options, CancellationToken ct) =>
            {
                SessionResult result = await mediator.Send(
                    new LoginCommand(request.Email ?? string.Empty, request.Password ?? string.Empty, SessionHttp.UserAgent(http)), ct);
                return SessionHttp.Respond(http, options.Value, result);
            })
            .RequireRateLimiting(RateLimitPolicies.Login);

        group.MapPost("/join", async (
                LoginRequest request, HttpContext http, IMediator mediator, IOptions<AuthOptions> options, CancellationToken ct) =>
            {
                SessionResult result = await mediator.Send(
                    new JoinTenantCommand(request.Email ?? string.Empty, request.Password ?? string.Empty, SessionHttp.UserAgent(http)), ct);
                return SessionHttp.Respond(http, options.Value, result);
            })
            .RequireRateLimiting(RateLimitPolicies.Login);

        group.MapPost("/refresh", async (
                HttpContext http, ITenantContext tenantContext, IMediator mediator, IOptions<AuthOptions> options, CancellationToken ct) =>
            {
                AuthScope scope = tenantContext.TenantId is null ? AuthScope.Platform : AuthScope.Tenant;
                return await RefreshAsync(http, scope, mediator, options.Value, ct);
            })
            .AddEndpointFilter<CookieRequestGuard>()
            .RequireRateLimiting(RateLimitPolicies.Refresh);

        group.MapPost("/logout", async (
                HttpContext http, ITenantContext tenantContext, IMediator mediator, IOptions<AuthOptions> options, CancellationToken ct) =>
            {
                AuthScope scope = tenantContext.TenantId is null ? AuthScope.Platform : AuthScope.Tenant;
                return await LogoutAsync(http, scope, mediator, options.Value, ct);
            })
            .AddEndpointFilter<CookieRequestGuard>()
            .RequireRateLimiting(RateLimitPolicies.Refresh);

        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, IMediator mediator, CancellationToken ct) =>
            {
                await mediator.Send(new ForgotPasswordCommand(request.Email ?? string.Empty), ct);
                return Results.Accepted();
            })
            .RequireRateLimiting(RateLimitPolicies.PasswordReset);

        group.MapPost("/reset-password", async (ResetPasswordRequest request, IMediator mediator, CancellationToken ct) =>
            {
                await mediator.Send(
                    new ResetPasswordCommand(
                        request.Token ?? string.Empty, request.NewPassword ?? string.Empty, request.ConfirmPassword ?? string.Empty),
                    ct);
                return Results.NoContent();
            })
            .RequireRateLimiting(RateLimitPolicies.PasswordReset);

        group.MapPost("/change-password", async (
                ChangePasswordRequest request, HttpContext http, ICurrentUser currentUser, IMediator mediator,
                IOptions<AuthOptions> options, CancellationToken ct) =>
            {
                string? refreshToken = currentUser.Scope is { } scope ? RefreshCookies.Read(http, scope) : null;
                SessionResult result = await mediator.Send(
                    new ChangePasswordCommand(
                        request.CurrentPassword ?? string.Empty,
                        request.NewPassword ?? string.Empty,
                        request.ConfirmPassword ?? string.Empty,
                        refreshToken),
                    ct);
                return SessionHttp.Respond(http, options.Value, result);
            })
            .RequireAuthorization(AuthPolicies.AnySession)
            .RequireRateLimiting(RateLimitPolicies.Login);

        group.MapGet("/me", async (IMediator mediator, HttpContext http, CancellationToken ct) =>
            {
                http.Response.Headers.CacheControl = "no-store";
                return Results.Ok(await mediator.Send(new GetMeQuery(), ct));
            })
            .RequireAuthorization(AuthPolicies.AnySession);

        return endpoints;
    }

    internal static async Task<IResult> RefreshAsync(
        HttpContext http, AuthScope scope, IMediator mediator, AuthOptions options, CancellationToken ct)
    {
        try
        {
            SessionResult result = await mediator.Send(
                new RefreshSessionCommand(RefreshCookies.Read(http, scope), scope, SessionHttp.UserAgent(http)), ct);
            return SessionHttp.Respond(http, options, result);
        }
        catch (ProblemException exception)
        {
            // Build the response here so the cleared cookie is not discarded by the exception handler.
            RefreshCookies.Clear(http, options, scope);
            return ProblemFactory.Result(http, exception);
        }
    }

    internal static async Task<IResult> LogoutAsync(
        HttpContext http, AuthScope scope, IMediator mediator, AuthOptions options, CancellationToken ct)
    {
        await mediator.Send(new LogoutCommand(RefreshCookies.Read(http, scope)), ct);
        RefreshCookies.Clear(http, options, scope);
        return Results.NoContent();
    }
}
