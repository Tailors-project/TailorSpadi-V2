using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TailorSpadi.Application.Features.SystemInfo;

namespace TailorSpadi.Api.Endpoints;

public static class SystemInfoEndpoints
{
    public static IEndpointRouteBuilder MapSystemInfoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/system-info", GetSystemInfoAsync)
            .WithName("GetSystemInfo")
            .WithTags("System");
        return endpoints;
    }

    private static async Task<Ok<SystemInfoResponse>> GetSystemInfoAsync(
        IMediator mediator, CancellationToken cancellationToken)
    {
        SystemInfoResponse response = await mediator.Send(new GetSystemInfoQuery(), cancellationToken);
        return TypedResults.Ok(response);
    }
}
