using TailorSpadi.Application.Options;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Api.Security;

/// <summary>
/// Refresh-token cookies: httpOnly, host-only (no Domain attribute, so one tenant's cookie is never sent to
/// another subdomain), SameSite=Strict, and a separate cookie per session scope.
/// </summary>
public static class RefreshCookies
{
    private const string CookiePath = "/api";

    public static string NameFor(AuthScope scope) => scope switch
    {
        AuthScope.Tenant => "ts_rt_t",
        AuthScope.Platform => "ts_rt_p",
        _ => "ts_rt_a"
    };

    public static string? Read(HttpContext context, AuthScope scope) =>
        context.Request.Cookies.TryGetValue(NameFor(scope), out string? value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    public static void Set(HttpContext context, AuthOptions options, AuthScope scope, string token, DateTime expiresUtc)
    {
        context.Response.Cookies.Append(NameFor(scope), token, Build(options, new DateTimeOffset(expiresUtc, TimeSpan.Zero)));
    }

    public static void Clear(HttpContext context, AuthOptions options, AuthScope scope)
    {
        context.Response.Cookies.Delete(NameFor(scope), Build(options, null));
    }

    private static CookieOptions Build(AuthOptions options, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = options.CookieSecure,
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,
        Expires = expires,
        IsEssential = true
    };
}
