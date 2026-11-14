using TailorSpadi.Application.Features.Identity;
using TailorSpadi.Application.Options;

namespace TailorSpadi.Api.Security;

public static class SessionHttp
{
    public static IResult Respond(HttpContext http, AuthOptions options, SessionResult result)
    {
        if (result.RefreshToken is not null && result.RefreshExpiresAtUtc is { } expires)
        {
            RefreshCookies.Set(http, options, result.Scope, result.RefreshToken, expires);
        }

        http.Response.Headers.CacheControl = "no-store";
        return Results.Ok(result.Response);
    }

    public static string UserAgent(HttpContext http) => http.Request.Headers.UserAgent.ToString();
}
