using System.Net;
using Microsoft.EntityFrameworkCore;
using TailorSpadi.Domain.Identity;
using TailorSpadi.IntegrationTests.Support;

namespace TailorSpadi.IntegrationTests;

public sealed class IdentityApiTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    [Fact]
    public async Task RegistrationRejectsDuplicateEmailIgnoringCaseAndWhitespace()
    {
        string email = TestApp.NewEmail();
        await App.RegisterTailorAsync(email);

        ApiResult duplicate = await App.PostAsync("localhost", "/api/auth/register-tailor", TestApp.RegisterBody("  " + email.ToUpperInvariant() + " "));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.Status);
        Assert.Equal("email_taken", duplicate.Code);
    }

    [Fact]
    public async Task RegistrationValidatesPasswordsAndFields()
    {
        ApiResult weak = await App.PostAsync("localhost", "/api/auth/register-tailor", TestApp.RegisterBody(TestApp.NewEmail(), "password123"));
        Assert.Equal(HttpStatusCode.BadRequest, weak.Status);
        Assert.True(weak.Json.GetProperty("errors").TryGetProperty("password", out _));

        ApiResult mismatch = await App.PostAsync("localhost", "/api/auth/register-tailor", new
        {
            firstName = "Ada",
            lastName = "Okafor",
            email = TestApp.NewEmail(),
            phone = "+2348012345678",
            password = TestApp.StrongPassword,
            confirmPassword = "Different#Passw0rd-1"
        });
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.Status);
        Assert.True(mismatch.Json.GetProperty("errors").TryGetProperty("confirmPassword", out _));

        ApiResult badEmail = await App.PostAsync("localhost", "/api/auth/register-tailor", TestApp.RegisterBody("not-an-email"));
        Assert.Equal(HttpStatusCode.BadRequest, badEmail.Status);
        Assert.True(badEmail.Json.GetProperty("errors").TryGetProperty("email", out _));
    }

    [Fact]
    public async Task PasswordIsStoredHashedNeverInPlainText()
    {
        Account account = await App.RegisterTailorAsync();

        await App.WithDbAsync(async db =>
        {
            User user = await db.Users.SingleAsync(candidate => candidate.NormalizedEmail == User.NormalizeEmail(account.Email));
            Assert.True(user.PasswordHash != account.Password);
            Assert.True(!user.PasswordHash.Contains(account.Password, StringComparison.Ordinal));
            Assert.True(user.PasswordHash.Length > 40);
        });
    }

    [Fact]
    public async Task TailorRegistrationIsRefusedOnATenantHost()
    {
        OnboardedTenant tenant = await App.OnboardTenantAsync();

        ApiResult result = await App.PostAsync($"{tenant.Subdomain}.localhost", "/api/auth/register-tailor", TestApp.RegisterBody(TestApp.NewEmail()));

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal("wrong_host", result.Code);
    }

    [Fact]
    public async Task LoginReturnsAPlatformSessionAndAHardenedRefreshCookie()
    {
        Account account = await App.RegisterTailorAsync();

        ApiResult result = await App.PostAsync("localhost", "/api/auth/login", new { email = account.Email, password = account.Password });

        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.Equal("platform", result.Str("session", "scope"));
        Assert.Equal(account.Email, result.Str("session", "user", "email"));
        Assert.True(result.Json.GetProperty("expiresInSeconds").GetInt32() <= 15 * 60);
        Assert.DoesNotContain("refresh", result.Body, StringComparison.OrdinalIgnoreCase);

        string header = result.SetCookieHeader("ts_rt_p")!.ToLowerInvariant();
        Assert.Contains("httponly", header);
        Assert.Contains("samesite=strict", header);
        Assert.Contains("path=/api", header);
        Assert.DoesNotContain("domain=", header);
        Assert.True(result.SetCookie("ts_rt_p")!.Length >= 43);
    }

    [Fact]
    public async Task LoginFailuresAreIndistinguishable()
    {
        Account account = await App.RegisterTailorAsync();

        ApiResult wrongPassword = await App.PostAsync("localhost", "/api/auth/login", new { email = account.Email, password = "Wrong#Password-99" });
        ApiResult unknownEmail = await App.PostAsync("localhost", "/api/auth/login", new { email = TestApp.NewEmail(), password = "Wrong#Password-99" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.Status);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.Status);
        Assert.Equal("invalid_credentials", wrongPassword.Code);
        Assert.Equal(wrongPassword.Str("title"), unknownEmail.Str("title"));
    }

    [Fact]
    public async Task RepeatedFailuresLockTheAccountEvenForTheRightPassword()
    {
        Account account = await App.RegisterTailorAsync();
        for (int attempt = 0; attempt < 5; attempt++)
        {
            ApiResult failed = await App.PostAsync("localhost", "/api/auth/login", new { email = account.Email, password = "Wrong#Password-99" });
            Assert.Equal(HttpStatusCode.Unauthorized, failed.Status);
        }

        ApiResult locked = await App.PostAsync("localhost", "/api/auth/login", new { email = account.Email, password = account.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, locked.Status);
        Assert.Equal("invalid_credentials", locked.Code);
    }

    [Fact]
    public async Task RefreshRotatesTheTokenAndReuseRevokesTheWholeFamily()
    {
        Account account = await App.RegisterTailorAsync();
        Session first = await App.LoginAsync("localhost", account);

        ApiResult rotated = await App.PostAsync("localhost", "/api/auth/refresh", cookie: first.RefreshCookie, cookieGuard: true);
        Assert.Equal(HttpStatusCode.OK, rotated.Status);
        string secondCookie = "ts_rt_p=" + rotated.SetCookie("ts_rt_p");
        Assert.True(secondCookie != first.RefreshCookie);

        ApiResult replay = await App.PostAsync("localhost", "/api/auth/refresh", cookie: first.RefreshCookie, cookieGuard: true);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.Status);
        Assert.Equal("invalid_session", replay.Code);
        Assert.Equal(string.Empty, replay.SetCookie("ts_rt_p"));

        ApiResult afterTheft = await App.PostAsync("localhost", "/api/auth/refresh", cookie: secondCookie, cookieGuard: true);
        Assert.Equal(HttpStatusCode.Unauthorized, afterTheft.Status);
    }

    [Fact]
    public async Task ConcurrentRefreshesOfOneTokenSucceedOnlyOnce()
    {
        Account account = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", account);

        ApiResult[] results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            App.PostAsync("localhost", "/api/auth/refresh", cookie: session.RefreshCookie, cookieGuard: true)));

        Assert.Equal(1, results.Count(result => result.Status == HttpStatusCode.OK));
        Assert.Equal(3, results.Count(result => result.Status == HttpStatusCode.Unauthorized));
    }

    [Fact]
    public async Task RefreshRequiresTheCsrfHeaderAndAMatchingOrigin()
    {
        Account account = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", account);

        ApiResult noHeaders = await App.PostAsync("localhost", "/api/auth/refresh", cookie: session.RefreshCookie);
        Assert.Equal(HttpStatusCode.Forbidden, noHeaders.Status);
        Assert.Equal("csrf_check_failed", noHeaders.Code);

        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/auth/refresh");
        request.Headers.Add("Cookie", session.RefreshCookie);
        request.Headers.Add("X-Requested-With", "TailorSpadi");
        request.Headers.Add("Origin", "http://evil.example");
        using HttpClient raw = Factory.CreateClient();
        using HttpResponseMessage foreignOrigin = await raw.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, foreignOrigin.StatusCode);

        // The refused attempts did not consume the token.
        ApiResult valid = await App.PostAsync("localhost", "/api/auth/refresh", cookie: session.RefreshCookie, cookieGuard: true);
        Assert.Equal(HttpStatusCode.OK, valid.Status);
    }

    [Fact]
    public async Task LogoutRevokesTheSessionAndClearsTheCookie()
    {
        Account account = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", account);

        ApiResult logout = await App.PostAsync("localhost", "/api/auth/logout", cookie: session.RefreshCookie, cookieGuard: true);
        Assert.Equal(HttpStatusCode.NoContent, logout.Status);
        Assert.Equal(string.Empty, logout.SetCookie("ts_rt_p"));

        ApiResult refresh = await App.PostAsync("localhost", "/api/auth/refresh", cookie: session.RefreshCookie, cookieGuard: true);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.Status);
    }

    [Fact]
    public async Task ProtectedEndpointsRejectMissingAndTamperedTokens()
    {
        Account account = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", account);

        Assert.Equal(HttpStatusCode.Unauthorized, (await App.GetAsync("localhost", "/api/auth/me")).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await App.GetAsync("localhost", "/api/auth/me", "not-a-token")).Status);

        string tampered = session.AccessToken[..^3] + (session.AccessToken.EndsWith("AAA", StringComparison.Ordinal) ? "BBB" : "AAA");
        Assert.Equal(HttpStatusCode.Unauthorized, (await App.GetAsync("localhost", "/api/auth/me", tampered)).Status);

        Assert.Equal(HttpStatusCode.OK, (await App.GetAsync("localhost", "/api/auth/me", session.AccessToken)).Status);
    }

    [Fact]
    public async Task PasswordChangeKeepsThisSessionAndRevokesTheOthers()
    {
        Account account = await App.RegisterTailorAsync();
        Session current = await App.LoginAsync("localhost", account);
        Session other = await App.LoginAsync("localhost", account);
        const string newPassword = "Brand-New#Passw0rd-77";

        ApiResult wrongCurrent = await App.PostAsync("localhost", "/api/auth/change-password",
            new { currentPassword = "Nope#Nope-12345", newPassword, confirmPassword = newPassword }, current.AccessToken);
        Assert.Equal("wrong_password", wrongCurrent.Code);

        ApiResult changed = await App.PostAsync("localhost", "/api/auth/change-password",
            new { currentPassword = account.Password, newPassword, confirmPassword = newPassword }, current.AccessToken, cookie: current.RefreshCookie);
        Assert.Equal(HttpStatusCode.OK, changed.Status);
        string renewedToken = changed.Str("accessToken");

        Assert.Equal(HttpStatusCode.Unauthorized, (await App.GetAsync("localhost", "/api/auth/me", current.AccessToken)).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await App.GetAsync("localhost", "/api/auth/me", other.AccessToken)).Status);
        Assert.Equal(HttpStatusCode.OK, (await App.GetAsync("localhost", "/api/auth/me", renewedToken)).Status);

        ApiResult keptRefresh = await App.PostAsync("localhost", "/api/auth/refresh", cookie: current.RefreshCookie, cookieGuard: true);
        Assert.Equal(HttpStatusCode.OK, keptRefresh.Status);
        ApiResult revokedRefresh = await App.PostAsync("localhost", "/api/auth/refresh", cookie: other.RefreshCookie, cookieGuard: true);
        Assert.Equal(HttpStatusCode.Unauthorized, revokedRefresh.Status);

        ApiResult oldLogin = await App.PostAsync("localhost", "/api/auth/login", new { email = account.Email, password = account.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.Status);
        ApiResult newLogin = await App.PostAsync("localhost", "/api/auth/login", new { email = account.Email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.Status);
    }

    [Fact]
    public async Task ForgotPasswordIsGenericAndResetLinksWorkOnce()
    {
        Account account = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", account);

        ApiResult unknown = await App.PostAsync("localhost", "/api/auth/forgot-password", new { email = TestApp.NewEmail() });
        ApiResult known = await App.PostAsync("localhost", "/api/auth/forgot-password", new { email = account.Email });
        Assert.Equal(HttpStatusCode.Accepted, unknown.Status);
        Assert.Equal(HttpStatusCode.Accepted, known.Status);
        Assert.Equal(unknown.Body, known.Body);
        Assert.Single(Factory.Emails.Messages);

        string token = Factory.Emails.LastResetTokenFor(account.Email)!;
        Assert.StartsWith("http://localhost", Factory.Emails.LastLinkFor(account.Email), StringComparison.Ordinal);
        const string newPassword = "Reset#Passw0rd-2026";

        ApiResult weak = await App.PostAsync("localhost", "/api/auth/reset-password",
            new { token, newPassword = "password123", confirmPassword = "password123" });
        Assert.Equal(HttpStatusCode.BadRequest, weak.Status);

        ApiResult reset = await App.PostAsync("localhost", "/api/auth/reset-password", new { token, newPassword, confirmPassword = newPassword });
        Assert.Equal(HttpStatusCode.NoContent, reset.Status);

        ApiResult reuse = await App.PostAsync("localhost", "/api/auth/reset-password", new { token, newPassword, confirmPassword = newPassword });
        Assert.Equal("reset_token_invalid", reuse.Code);

        // Every session of the account was revoked by the reset.
        Assert.Equal(HttpStatusCode.Unauthorized, (await App.GetAsync("localhost", "/api/auth/me", session.AccessToken)).Status);
        ApiResult oldRefresh = await App.PostAsync("localhost", "/api/auth/refresh", cookie: session.RefreshCookie, cookieGuard: true);
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefresh.Status);

        ApiResult oldLogin = await App.PostAsync("localhost", "/api/auth/login", new { email = account.Email, password = account.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.Status);
        ApiResult newLogin = await App.PostAsync("localhost", "/api/auth/login", new { email = account.Email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.Status);
    }

    [Fact]
    public async Task ResetTokensAreStoredHashedAndGarbageTokensFail()
    {
        Account account = await App.RegisterTailorAsync();
        await App.PostAsync("localhost", "/api/auth/forgot-password", new { email = account.Email });
        string token = Factory.Emails.LastResetTokenFor(account.Email)!;

        await App.WithDbAsync(async db =>
        {
            List<string> hashes = await db.PasswordResetTokens.Select(row => row.TokenHash).ToListAsync();
            Assert.Single(hashes);
            Assert.True(hashes[0] != token);
        });

        ApiResult garbage = await App.PostAsync("localhost", "/api/auth/reset-password",
            new { token = "garbage", newPassword = "Reset#Passw0rd-2026", confirmPassword = "Reset#Passw0rd-2026" });
        Assert.Equal("reset_token_invalid", garbage.Code);
    }

    [Fact]
    public async Task DeactivatedAccountsLoseEveryKindOfAccess()
    {
        Account account = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", account);

        await App.WithDbAsync(async db =>
        {
            User user = await db.Users.SingleAsync(candidate => candidate.NormalizedEmail == User.NormalizeEmail(account.Email));
            user.IsActive = false;
            await db.SaveChangesAsync();
        });

        Assert.Equal(HttpStatusCode.Unauthorized, (await App.GetAsync("localhost", "/api/auth/me", session.AccessToken)).Status);
        ApiResult login = await App.PostAsync("localhost", "/api/auth/login", new { email = account.Email, password = account.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, login.Status);
        ApiResult refresh = await App.PostAsync("localhost", "/api/auth/refresh", cookie: session.RefreshCookie, cookieGuard: true);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.Status);
    }

    [Fact]
    public async Task AuditTrailRecordsKeyEventsWithoutSecrets()
    {
        Account account = await App.RegisterTailorAsync();
        await App.PostAsync("localhost", "/api/auth/login", new { email = account.Email, password = "Wrong#Password-99" });
        await App.LoginAsync("localhost", account);

        await App.WithDbAsync(async db =>
        {
            List<AuditLogEntry> entries = await db.AuditLog.ToListAsync();
            string[] actions = entries.Select(entry => entry.Action).ToArray();
            Assert.Contains("user.registered", actions);
            Assert.Contains("login.failed", actions);
            Assert.Contains("login.succeeded", actions);
            Assert.True(entries.All(entry => !entry.Detail.Contains(account.Password, StringComparison.Ordinal)));
        });
    }
}
