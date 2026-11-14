using TailorSpadi.Api.Errors;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Tenancy;

namespace TailorSpadi.Api.Tenancy;

/// <summary>
/// Resolves the tenant from the accepted request host (never from forwarded host headers, query strings or
/// bodies) and fills the per-request tenant context. Unknown or unavailable tenants stop the request here.
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantResolver resolver, TenantContext tenantContext)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        TenantResolution result = await resolver.ResolveAsync(context.Request.Host.Value, context.RequestAborted);
        switch (result.Status)
        {
            case TenantResolutionStatus.Platform:
                break;
            case TenantResolutionStatus.Resolved:
                tenantContext.Set(result.Tenant);
                break;
            case TenantResolutionStatus.Suspended:
                await Reject(context, ProblemException.Forbidden("tenant_suspended", "This store is temporarily unavailable."));
                return;
            case TenantResolutionStatus.NotFound:
                await Reject(context, ProblemException.NotFound("tenant_not_found", "This store was not found."));
                return;
            default:
                await Reject(context, ProblemException.BadRequest("invalid_host", "The host name is not valid."));
                return;
        }

        await next(context);
    }

    private static Task Reject(HttpContext context, ProblemException exception) =>
        ProblemFactory.Result(context, exception).ExecuteAsync(context);
}
