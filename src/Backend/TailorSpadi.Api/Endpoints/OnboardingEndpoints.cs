using Mediator;
using Microsoft.Extensions.Options;
using TailorSpadi.Api.Contracts;
using TailorSpadi.Api.Errors;
using TailorSpadi.Api.Security;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Features.Onboarding;
using TailorSpadi.Application.Options;

namespace TailorSpadi.Api.Endpoints;

public static class OnboardingEndpoints
{
    public const int RequestSizeLimitBytes = 3 * 1024 * 1024;

    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/onboarding")
            .WithTags("Onboarding")
            .RequireAuthorization(AuthPolicies.PlatformUser);

        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetOnboardingQuery(), ct)));

        group.MapPut("/business", async (BusinessRequest request, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new SaveBusinessInfoCommand(request.ToBusinessInfo()), ct)));

        group.MapPut("/branding", async (BrandingRequest request, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(
                new SaveBrandingCommand(request.PrimaryColor ?? string.Empty, request.SecondaryColor ?? string.Empty), ct)));

        group.MapPost("/logo", async (HttpRequest request, IMediator mediator, IOptions<StorageOptions> storage, CancellationToken ct) =>
            {
                byte[]? content = await LogoUpload.ReadAsync(request, storage.Value, ct);
                if (content is null)
                {
                    return ProblemFactory.Result(request.HttpContext, ProblemException.BadRequest("invalid_image", "Choose an image file."));
                }

                return Results.Ok(await mediator.Send(new UploadLogoCommand(content), ct));
            })
            .WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(RequestSizeLimitBytes))
            .RequireRateLimiting(RateLimitPolicies.Upload);

        group.MapPost("/subdomain/preview", async (SubdomainPreviewRequest request, IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new PreviewSubdomainQuery(request.SlugBase), ct)))
            .RequireRateLimiting(RateLimitPolicies.Subdomain);

        group.MapPut("/subdomain", async (SubdomainPreviewRequest request, IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new SaveSubdomainCommand(request.SlugBase), ct)))
            .RequireRateLimiting(RateLimitPolicies.Subdomain);

        group.MapPost("/complete", async (CompleteOnboardingRequest request, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new CompleteOnboardingCommand(request.DraftId, request.SlugBase), ct)));

        return endpoints;
    }
}
