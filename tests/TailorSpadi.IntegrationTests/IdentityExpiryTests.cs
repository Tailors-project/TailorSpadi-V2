using System.Net;
using TailorSpadi.IntegrationTests.Support;

namespace TailorSpadi.IntegrationTests;

public sealed class IdentityExpiryTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    private readonly MutableTimeProvider _clock = new();

    protected override TimeProvider? Clock => _clock;

    [Fact]
    public async Task ResetLinksExpireAfterThirtyMinutes()
    {
        Account account = await App.RegisterTailorAsync();
        await App.PostAsync("localhost", "/api/auth/forgot-password", new { email = account.Email });
        string token = Factory.Emails.LastResetTokenFor(account.Email)!;

        _clock.Advance(TimeSpan.FromMinutes(31));
        ApiResult expired = await App.PostAsync("localhost", "/api/auth/reset-password",
            new { token, newPassword = "Reset#Passw0rd-2026", confirmPassword = "Reset#Passw0rd-2026" });

        Assert.Equal("reset_token_invalid", expired.Code);
    }

    [Fact]
    public async Task RefreshTokensExpireAfterFourteenDays()
    {
        Account account = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", account);

        _clock.Advance(TimeSpan.FromDays(15));
        ApiResult expired = await App.PostAsync("localhost", "/api/auth/refresh", cookie: session.RefreshCookie, cookieGuard: true);

        Assert.Equal(HttpStatusCode.Unauthorized, expired.Status);
        Assert.Equal("invalid_session", expired.Code);
    }

    [Fact]
    public async Task LockoutEndsAfterTheLockoutPeriod()
    {
        Account account = await App.RegisterTailorAsync();
        for (int attempt = 0; attempt < 5; attempt++)
        {
            await App.PostAsync("localhost", "/api/auth/login", new { email = account.Email, password = "Wrong#Password-99" });
        }

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await App.PostAsync("localhost", "/api/auth/login", new { email = account.Email, password = account.Password })).Status);

        _clock.Advance(TimeSpan.FromMinutes(16));
        ApiResult afterLockout = await App.PostAsync("localhost", "/api/auth/login", new { email = account.Email, password = account.Password });

        Assert.Equal(HttpStatusCode.OK, afterLockout.Status);
    }
}
