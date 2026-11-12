using System.Net;
using TailorSpadi.IntegrationTests.Support;

namespace TailorSpadi.IntegrationTests;

public sealed class OnboardingApiTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    private static object Business(string email, string name = "Stitch Perfect") => new
    {
        businessName = name,
        description = "d",
        phone = "+2348012345678",
        whatsApp = "",
        email,
        address = "1 Road",
        city = "Lagos",
        state = "Lagos",
        country = "Nigeria"
    };

    [Fact]
    public async Task OnboardingRequiresAPlatformSession()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await App.GetAsync("localhost", "/api/onboarding")).Status);

        OnboardedTenant tenant = await App.OnboardTenantAsync();
        Session tenantSession = await App.LoginAsync($"{tenant.Subdomain}.localhost", tenant.Owner);
        ApiResult result = await App.GetAsync("localhost", "/api/onboarding", tenantSession.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
    }

    [Fact]
    public async Task NewUserHasNoDraftAndResumeReturnsTheSavedStep()
    {
        Account owner = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", owner);

        ApiResult empty = await App.GetAsync("localhost", "/api/onboarding", session.AccessToken);
        Assert.False(empty.Json.GetProperty("hasDraft").GetBoolean());
        Assert.Equal(1, empty.Json.GetProperty("step").GetInt32());

        ApiResult saved = await App.PutAsync("localhost", "/api/onboarding/business", Business(owner.Email), session.AccessToken);
        Assert.Equal(HttpStatusCode.OK, saved.Status);
        Assert.Equal(2, saved.Json.GetProperty("step").GetInt32());

        ApiResult resumed = await App.GetAsync("localhost", "/api/onboarding", session.AccessToken);
        Assert.True(resumed.Json.GetProperty("hasDraft").GetBoolean());
        Assert.Equal("Stitch Perfect", resumed.Str("business", "businessName"));
    }

    [Fact]
    public async Task BusinessInfoIsValidated()
    {
        Account owner = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", owner);

        ApiResult result = await App.PutAsync("localhost", "/api/onboarding/business", Business(owner.Email, "x"), session.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.True(result.Json.GetProperty("errors").TryGetProperty("businessName", out _));
    }

    [Fact]
    public async Task BrandingRejectsInvalidColors()
    {
        Account owner = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", owner);
        await App.PutAsync("localhost", "/api/onboarding/business", Business(owner.Email), session.AccessToken);

        ApiResult result = await App.PutAsync("localhost", "/api/onboarding/branding",
            new { primaryColor = "red", secondaryColor = "#AABBCC" }, session.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
    }

    [Fact]
    public async Task CompletionCreatesTheTenantWithANumberedSubdomainAndIsIdempotent()
    {
        Account owner = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", owner);
        ApiResult business = await App.PutAsync("localhost", "/api/onboarding/business", Business(owner.Email, "John & Sons Fashion"), session.AccessToken);
        string draftId = business.Str("draftId");
        await App.PutAsync("localhost", "/api/onboarding/branding",
            new { primaryColor = "#112233", secondaryColor = "#AABBCC" }, session.AccessToken);

        ApiResult first = await App.PostAsync("localhost", "/api/onboarding/complete", new { draftId }, session.AccessToken);
        ApiResult second = await App.PostAsync("localhost", "/api/onboarding/complete", new { draftId }, session.AccessToken);

        Assert.Equal(HttpStatusCode.OK, first.Status);
        Assert.Matches("^john-and-sons-fashion-\\d{3}$", first.Str("subdomain"));
        Assert.Equal(first.Str("subdomain"), second.Str("subdomain"));
        Assert.Equal(1, await CountTenantsAsync());

        ApiResult state = await App.GetAsync("localhost", "/api/onboarding", session.AccessToken);
        Assert.False(state.Json.GetProperty("hasDraft").GetBoolean());
    }

    [Fact]
    public async Task ConcurrentCompletionsCreateASingleTenant()
    {
        Account owner = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", owner);
        string draftId = (await App.PutAsync("localhost", "/api/onboarding/business", Business(owner.Email), session.AccessToken)).Str("draftId");
        await App.PutAsync("localhost", "/api/onboarding/branding",
            new { primaryColor = "#112233", secondaryColor = "#AABBCC" }, session.AccessToken);

        ApiResult[] results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            App.PostAsync("localhost", "/api/onboarding/complete", new { draftId }, session.AccessToken)));

        Assert.All(results, result => Assert.Equal(HttpStatusCode.OK, result.Status));
        Assert.Equal(1, await CountTenantsAsync());
    }

    [Fact]
    public async Task ConcurrentBusinessesWithTheSameNameGetDistinctSubdomains()
    {
        OnboardedTenant[] tenants = await Task.WhenAll(
            App.OnboardTenantAsync("Stitch Perfect"), App.OnboardTenantAsync("Stitch Perfect"));
        Assert.NotEqual(tenants[0].Subdomain, tenants[1].Subdomain);
    }

    [Fact]
    public async Task ReservedNamesAreRejectedInPreview()
    {
        Account owner = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", owner);
        await App.PutAsync("localhost", "/api/onboarding/business", Business(owner.Email), session.AccessToken);

        ApiResult result = await App.PostAsync("localhost", "/api/onboarding/subdomain/preview", new { slugBase = "admin" }, session.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal("reserved_name", result.Code);
    }

    [Fact]
    public async Task CompletingSomeoneElsesDraftIsRefused()
    {
        Account owner = await App.RegisterTailorAsync();
        Session ownerSession = await App.LoginAsync("localhost", owner);
        string draftId = (await App.PutAsync("localhost", "/api/onboarding/business", Business(owner.Email), ownerSession.AccessToken)).Str("draftId");

        Account other = await App.RegisterTailorAsync();
        Session otherSession = await App.LoginAsync("localhost", other);
        ApiResult result = await App.PostAsync("localhost", "/api/onboarding/complete", new { draftId }, otherSession.AccessToken);

        Assert.True(result.Status is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest);
        Assert.Equal(0, await CountTenantsAsync());
    }

    private async Task<int> CountTenantsAsync()
    {
        int count = 0;
        await App.WithDbAsync(async db => count = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
            db.Tenants.Where(tenant => tenant.Subdomain != null)));
        return count;
    }
}
