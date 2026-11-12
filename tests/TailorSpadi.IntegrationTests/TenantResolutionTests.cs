using System.Net;
using TailorSpadi.Domain.Tenancy;
using TailorSpadi.IntegrationTests.Support;

namespace TailorSpadi.IntegrationTests;

public sealed class TenantResolutionTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    [Fact]
    public async Task ActiveTenantHostServesPublicBranding()
    {
        OnboardedTenant tenant = await App.OnboardTenantAsync("Stitch Perfect");

        ApiResult result = await App.GetAsync($"{tenant.Subdomain}.localhost", "/api/tenant/current");

        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.Equal("Stitch Perfect", result.Str("name"));
        Assert.Equal("#112233", result.Str("primaryColor"));
    }

    [Fact]
    public async Task UnknownHostIsNotFound()
    {
        ApiResult result = await App.GetAsync("nobody-here.localhost", "/api/tenant/current");

        Assert.Equal(HttpStatusCode.NotFound, result.Status);
        Assert.Equal("tenant_not_found", result.Code);
    }

    [Fact]
    public async Task PlatformHostHasNoTenant()
    {
        ApiResult result = await App.GetAsync("localhost", "/api/tenant/current");

        Assert.Equal(HttpStatusCode.NotFound, result.Status);
    }

    [Fact]
    public async Task IncompleteOnboardingTenantIsNotFound()
    {
        Account owner = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", owner);
        await App.PutAsync("localhost", "/api/onboarding/business", new
        {
            businessName = "Draft Only",
            description = "",
            phone = "+2348012345678",
            whatsApp = "",
            email = owner.Email,
            address = "",
            city = "",
            state = "",
            country = "Nigeria"
        }, session.AccessToken);

        ApiResult result = await App.GetAsync("draft-only.localhost", "/api/tenant/current");

        Assert.Equal(HttpStatusCode.NotFound, result.Status);
    }

    [Fact]
    public async Task SuspendedAndDeactivatedTenantsAreForbiddenAndRecoverAfterReactivation()
    {
        OnboardedTenant tenant = await App.OnboardTenantAsync();
        string host = $"{tenant.Subdomain}.localhost";
        Assert.Equal(HttpStatusCode.OK, (await App.GetAsync(host, "/api/tenant/current")).Status);

        await App.SetTenantStatusAsync(tenant.Subdomain, TenantStatus.Suspended);
        Factory.InvalidateTenantCache(tenant.Subdomain);
        ApiResult suspended = await App.GetAsync(host, "/api/tenant/current");
        Assert.Equal(HttpStatusCode.Forbidden, suspended.Status);
        Assert.Equal("tenant_suspended", suspended.Code);

        await App.SetTenantStatusAsync(tenant.Subdomain, TenantStatus.Deactivated);
        Factory.InvalidateTenantCache(tenant.Subdomain);
        Assert.Equal(HttpStatusCode.Forbidden, (await App.GetAsync(host, "/api/tenant/current")).Status);

        await App.SetTenantStatusAsync(tenant.Subdomain, TenantStatus.Active);
        Factory.InvalidateTenantCache(tenant.Subdomain);
        Assert.Equal(HttpStatusCode.OK, (await App.GetAsync(host, "/api/tenant/current")).Status);
    }

    [Fact]
    public async Task ForwardedHostHeaderCannotChangeTheTenant()
    {
        OnboardedTenant tenant = await App.OnboardTenantAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/tenant/current");
        request.Headers.Add("X-Forwarded-Host", $"{tenant.Subdomain}.localhost");
        using HttpClient client = Factory.CreateClient();

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TenantTokenIsRejectedOnAnotherTenantHost()
    {
        OnboardedTenant a = await App.OnboardTenantAsync("Alpha Wear");
        OnboardedTenant b = await App.OnboardTenantAsync("Beta Wear");
        Session session = await App.LoginAsync($"{a.Subdomain}.localhost", a.Owner);

        Assert.Equal(HttpStatusCode.OK, (await App.GetAsync($"{a.Subdomain}.localhost", "/api/auth/me", session.AccessToken)).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await App.GetAsync($"{b.Subdomain}.localhost", "/api/auth/me", session.AccessToken)).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await App.GetAsync("localhost", "/api/auth/me", session.AccessToken)).Status);
    }

    [Fact]
    public async Task HealthEndpointsWorkOnAnyHost()
    {
        Assert.Equal(HttpStatusCode.OK, (await App.GetAsync("unknown-store.localhost", "/health/live")).Status);
    }
}
