using Mediator;
using Microsoft.Extensions.Options;
using TailorSpadi.Api.Errors;
using TailorSpadi.Api.Security;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Features.Tenants;
using TailorSpadi.Application.Options;

namespace TailorSpadi.Api.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/tenant").WithTags("Tenant");

        group.MapGet("/current", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetCurrentTenantQuery(), ct)));

        group.MapGet("/current/logo", async (HttpContext http, IMediator mediator, CancellationToken ct) =>
        {
            LogoFile logo = await mediator.Send(new GetTenantLogoQuery(), ct);

            // The URL carries a version, so a long cache lifetime is safe; a new logo gets a new URL.
            http.Response.Headers.CacheControl = "public, max-age=86400";
            return Results.Bytes(logo.Content, logo.ContentType);
        });

        group.MapPost("/logo", async (HttpRequest request, IMediator mediator, IOptions<StorageOptions> storage, CancellationToken ct) =>
            {
                byte[]? content = await LogoUpload.ReadAsync(request, storage.Value, ct);
                if (content is null)
                {
                    return ProblemFactory.Result(request.HttpContext, ProblemException.BadRequest("invalid_image", "Choose an image file."));
                }

                return Results.Ok(await mediator.Send(new ReplaceTenantLogoCommand(content), ct));
            })
            .WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(OnboardingEndpoints.RequestSizeLimitBytes))
            .RequireAuthorization(AuthPolicies.ManageBusiness)
            .RequireRateLimiting(RateLimitPolicies.Upload);

        return endpoints;
    }
}
