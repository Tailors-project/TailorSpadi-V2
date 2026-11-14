using Microsoft.Extensions.Options;
using TailorSpadi.Api.Errors;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Options;

namespace TailorSpadi.Api.Security;

/// <summary>
/// CSRF defence for the cookie-authenticated endpoints (refresh and logout): a required custom header that
/// cross-site forms cannot send, plus an Origin that must exactly match the host that received the request
/// (or a configured exact origin). SameSite=Strict on the cookie is the third layer.
/// </summary>
public sealed class CookieRequestGuard(IOptions<AuthOptions> options) : IEndpointFilter
{
    public const string HeaderName = "X-Requested-With";
    public const string HeaderValue = "TailorSpadi";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        HttpContext http = context.HttpContext;
        HttpRequest request = http.Request;

        bool headerOk = request.Headers.TryGetValue(HeaderName, out var header) && header == HeaderValue;
        bool originOk = request.Headers.Origin.Count == 1 && OriginAllowed(request.Headers.Origin[0], request);
        if (!headerOk || !originOk)
        {
            return ProblemFactory.Result(
                http, ProblemException.Forbidden("csrf_check_failed", "The request could not be verified."));
        }

        return await next(context);
    }

    private bool OriginAllowed(string? origin, HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(origin) || !Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (options.Value.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        bool sameScheme = string.Equals(uri.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase);
        bool sameAuthority = string.Equals(uri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);
        return sameScheme && sameAuthority;
    }
}
