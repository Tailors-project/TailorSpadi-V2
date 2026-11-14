using System.Net;
using TailorSpadi.IntegrationTests.Support;

namespace TailorSpadi.IntegrationTests;

public sealed class AdminSeparationTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    [Fact]
    public async Task PlatformAdminLogsInOnlyThroughTheAdminEndpointOnThePlatformHost()
    {
        Account admin = await App.CreateAdminAsync();

        ApiResult viaAdmin = await App.PostAsync("localhost", "/api/admin/auth/login", new { email = admin.Email, password = admin.Password });
        Assert.Equal(HttpStatusCode.OK, viaAdmin.Status);
        Assert.Equal("admin", viaAdmin.Str("session", "scope"));
        Assert.NotNull(viaAdmin.SetCookie("ts_rt_a"));
        Assert.Null(viaAdmin.SetCookie("ts_rt_p"));

        ApiResult viaOrdinary = await App.PostAsync("localhost", "/api/auth/login", new { email = admin.Email, password = admin.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, viaOrdinary.Status);
        Assert.Null(viaOrdinary.SetCookie("ts_rt_p"));

        OnboardedTenant tenant = await App.OnboardTenantAsync();
        ApiResult onTenant = await App.PostAsync($"{tenant.Subdomain}.localhost", "/api/admin/auth/login", new { email = admin.Email, password = admin.Password });
        Assert.NotEqual(HttpStatusCode.OK, onTenant.Status);
    }

    [Fact]
    public async Task OrdinaryUsersCannotUseTheAdminLogin()
    {
        Account tailor = await App.RegisterTailorAsync();

        ApiResult result = await App.PostAsync("localhost", "/api/admin/auth/login", new { email = tailor.Email, password = tailor.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        Assert.Equal("invalid_credentials", result.Code);
    }

    [Fact]
    public async Task AdminEndpointRefusesNonAdminTokens()
    {
        Account admin = await App.CreateAdminAsync();
        Session adminSession = await App.LoginAsync("localhost", admin, "/api/admin/auth/login");
        Account tailor = await App.RegisterTailorAsync();
        Session platformSession = await App.LoginAsync("localhost", tailor);
        OnboardedTenant tenant = await App.OnboardTenantAsync();
        Session tenantSession = await App.LoginAsync($"{tenant.Subdomain}.localhost", tenant.Owner);

        Assert.Equal(HttpStatusCode.OK, (await App.GetAsync("localhost", "/api/admin/auth/me", adminSession.AccessToken)).Status);
        Assert.True((await App.GetAsync("localhost", "/api/admin/auth/me", platformSession.AccessToken)).Status is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized);
        Assert.True((await App.GetAsync($"{tenant.Subdomain}.localhost", "/api/admin/auth/me", tenantSession.AccessToken)).Status is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminTokenIsNotAcceptedForOnboardingAndRefreshUsesItsOwnCookie()
    {
        Account admin = await App.CreateAdminAsync();
        Session session = await App.LoginAsync("localhost", admin, "/api/admin/auth/login");

        Assert.Equal(HttpStatusCode.Forbidden, (await App.GetAsync("localhost", "/api/onboarding", session.AccessToken)).Status);

        ApiResult wrongEndpoint = await App.PostAsync("localhost", "/api/auth/refresh", cookie: session.RefreshCookie, cookieGuard: true);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongEndpoint.Status);

        ApiResult refreshed = await App.PostAsync("localhost", "/api/admin/auth/refresh", cookie: session.RefreshCookie, cookieGuard: true);
        Assert.Equal(HttpStatusCode.OK, refreshed.Status);
    }
}
