using System.Net;
using TailorSpadi.IntegrationTests.Support;

namespace TailorSpadi.IntegrationTests;

public sealed class LogoUploadTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    private async Task<(Session Session, string DraftOwnerEmail)> DraftAsync()
    {
        Account owner = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", owner);
        await App.PutAsync("localhost", "/api/onboarding/business", new
        {
            businessName = "Logo Test",
            description = "",
            phone = "+2348012345678",
            whatsApp = "",
            email = owner.Email,
            address = "",
            city = "",
            state = "",
            country = "Nigeria"
        }, session.AccessToken);
        return (session, owner.Email);
    }

    [Theory]
    [InlineData("png")]
    [InlineData("jpeg")]
    [InlineData("webp")]
    public async Task AcceptedFormatsAreStoredReEncodedAsPng(string format)
    {
        (Session session, _) = await DraftAsync();
        byte[] bytes = format switch { "png" => TestImages.Png(1200, 600), "jpeg" => TestImages.Jpeg(), _ => TestImages.Webp() };

        ApiResult result = await App.UploadAsync("localhost", "/api/onboarding/logo", bytes, "logo." + format, "image/" + format, session.AccessToken);

        Assert.Equal(HttpStatusCode.OK, result.Status);
        string preview = result.Str("branding", "logoPreviewDataUrl");
        Assert.StartsWith("data:image/png;base64,", preview, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NonImagesAndDisguisedFilesAreRejected()
    {
        (Session session, _) = await DraftAsync();

        ApiResult text = await App.UploadAsync("localhost", "/api/onboarding/logo", "hello"u8.ToArray(), "logo.png", "image/png", session.AccessToken);
        ApiResult svg = await App.UploadAsync("localhost", "/api/onboarding/logo", "<svg xmlns='http://www.w3.org/2000/svg'/>"u8.ToArray(), "logo.svg", "image/svg+xml", session.AccessToken);
        ApiResult truncated = await App.UploadAsync("localhost", "/api/onboarding/logo", TestImages.Png()[..40], "logo.png", "image/png", session.AccessToken);

        Assert.Equal("invalid_image", text.Code);
        Assert.Equal("invalid_image", svg.Code);
        Assert.Equal(HttpStatusCode.BadRequest, truncated.Status);
    }

    [Fact]
    public async Task OversizedUploadsAreRejected()
    {
        (Session session, _) = await DraftAsync();
        byte[] huge = new byte[6 * 1024 * 1024];
        TestImages.Png().CopyTo(huge, 0);

        ApiResult result = await App.UploadAsync("localhost", "/api/onboarding/logo", huge, "logo.png", "image/png", session.AccessToken);

        Assert.True(result.Status is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task TenantLogoIsServedPubliclyAndCanBeReplacedByTheOwner()
    {
        OnboardedTenant tenant = await App.OnboardTenantAsync();
        string host = $"{tenant.Subdomain}.localhost";
        Session owner = await App.LoginAsync(host, tenant.Owner);

        ApiResult noLogo = await App.GetAsync(host, "/api/tenant/current/logo");
        Assert.Equal(HttpStatusCode.NotFound, noLogo.Status);

        ApiResult replaced = await App.UploadAsync(host, "/api/tenant/logo", TestImages.Jpeg(), "l.jpg", "image/jpeg", owner.AccessToken);
        Assert.Equal(HttpStatusCode.OK, replaced.Status);

        ApiResult served = await App.GetAsync(host, "/api/tenant/current/logo");
        Assert.Equal(HttpStatusCode.OK, served.Status);
        Assert.Equal("image/png", served.Response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", served.Response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task LogoUploadNeedsAnOwnerSession()
    {
        OnboardedTenant tenant = await App.OnboardTenantAsync();

        ApiResult result = await App.UploadAsync($"{tenant.Subdomain}.localhost", "/api/tenant/logo", TestImages.Png(), "l.png", "image/png", "bad");

        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
    }
}
