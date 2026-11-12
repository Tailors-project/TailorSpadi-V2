using System.Net;
using TailorSpadi.IntegrationTests.Support;

namespace TailorSpadi.IntegrationTests;

public sealed class RateLimitTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    protected override IDictionary<string, string?>? Overrides => new Dictionary<string, string?> { ["RateLimiting:LoginPerMinute"] = "3" };

    [Fact]
    public async Task LoginIsThrottledWithRetryAfter()
    {
        ApiResult? throttled = null;
        for (int attempt = 0; attempt < 6 && throttled is null; attempt++)
        {
            ApiResult result = await App.PostAsync("localhost", "/api/auth/login", new { email = TestApp.NewEmail(), password = "Wrong#Password-99" });
            if (result.Status == HttpStatusCode.TooManyRequests)
            {
                throttled = result;
            }
        }

        Assert.NotNull(throttled);
        Assert.True(throttled!.Response.Headers.Contains("Retry-After"));
    }
}
