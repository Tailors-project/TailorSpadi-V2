using System.Net;
using System.Text.Json;

namespace TailorSpadi.IntegrationTests.Support;

public sealed class ApiResult(HttpStatusCode status, string body, HttpResponseMessage response)
{
    public HttpStatusCode Status { get; } = status;
    public string Body { get; } = body;
    public HttpResponseMessage Response { get; } = response;

    public JsonElement Json => string.IsNullOrEmpty(Body) ? default : JsonDocument.Parse(Body).RootElement;

    /// <summary>The machine-readable problem code, when the response is a problem document.</summary>
    public string? Code => Json.ValueKind == JsonValueKind.Object && Json.TryGetProperty("code", out JsonElement code)
        ? code.GetString()
        : null;

    public string Str(params string[] path)
    {
        JsonElement current = Json;
        foreach (string segment in path)
        {
            current = current.GetProperty(segment);
        }

        return current.GetString()!;
    }

    /// <summary>Value of a Set-Cookie header by name; the empty string when the cookie was cleared; null when absent.</summary>
    public string? SetCookie(string name)
    {
        if (!Response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values))
        {
            return null;
        }

        foreach (string header in values)
        {
            string first = header.Split(';')[0];
            int equals = first.IndexOf('=', StringComparison.Ordinal);
            if (equals > 0 && first[..equals] == name)
            {
                return first[(equals + 1)..];
            }
        }

        return null;
    }

    public string? SetCookieHeader(string name) => Response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values)
        ? values.FirstOrDefault(header => header.StartsWith(name + "=", StringComparison.Ordinal))
        : null;
}
