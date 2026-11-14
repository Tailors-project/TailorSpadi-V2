using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;
using TailorSpadi.Infrastructure.Persistence;

namespace TailorSpadi.IntegrationTests.Support;

public sealed record Account(string Email, string Password);

public sealed record OnboardedTenant(Account Owner, string Subdomain, Guid TenantId, string PlatformToken);

public sealed record Session(string AccessToken, string RefreshCookie);

/// <summary>A thin HTTP helper that sends requests to chosen hosts and handles cookies explicitly.</summary>
public sealed class TestApp : IDisposable
{
    public const string StrongPassword = "Sew-Tailor#Lagos-2026";

    private readonly HttpClient _client;

    public TestApp(ApiFactory factory)
    {
        Factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
    }

    public ApiFactory Factory { get; }

    public static string NewEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@example.test";

    public async Task EnsureDatabaseAsync()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public async Task<ApiResult> SendAsync(
        HttpMethod method,
        string host,
        string path,
        object? body = null,
        string? bearer = null,
        string? cookie = null,
        bool cookieGuard = false,
        HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, $"http://{host}{path}");
        if (content is not null)
        {
            request.Content = content;
        }
        else if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (bearer is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        if (cookie is not null)
        {
            request.Headers.Add("Cookie", cookie);
        }

        if (cookieGuard)
        {
            request.Headers.Add("X-Requested-With", "TailorSpadi");
            request.Headers.Add("Origin", $"http://{host}");
        }

        HttpResponseMessage response = await _client.SendAsync(request);
        string text = await response.Content.ReadAsStringAsync();
        return new ApiResult(response.StatusCode, text, response);
    }

    public Task<ApiResult> PostAsync(string host, string path, object? body = null, string? bearer = null, string? cookie = null, bool cookieGuard = false) =>
        SendAsync(HttpMethod.Post, host, path, body, bearer, cookie, cookieGuard);

    public Task<ApiResult> GetAsync(string host, string path, string? bearer = null) =>
        SendAsync(HttpMethod.Get, host, path, bearer: bearer);

    public Task<ApiResult> PutAsync(string host, string path, object body, string? bearer = null) =>
        SendAsync(HttpMethod.Put, host, path, body, bearer);

    public async Task<ApiResult> UploadAsync(string host, string path, byte[] bytes, string fileName, string contentType, string bearer)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return await SendAsync(HttpMethod.Post, host, path, bearer: bearer, content: form);
    }

    public static object RegisterBody(string email, string password = StrongPassword) => new
    {
        firstName = "Ada",
        lastName = "Okafor",
        email,
        phone = "+2348012345678",
        password,
        confirmPassword = password
    };

    public async Task<Account> RegisterTailorAsync(string? email = null, string password = StrongPassword)
    {
        email ??= NewEmail("tailor");
        ApiResult result = await PostAsync("localhost", "/api/auth/register-tailor", RegisterBody(email, password));
        Assert.Equal(System.Net.HttpStatusCode.Created, result.Status);
        return new Account(email, password);
    }

    public async Task<Session> LoginAsync(string host, Account account, string path = "/api/auth/login")
    {
        ApiResult result = await PostAsync(host, path, new { email = account.Email, password = account.Password });
        Assert.Equal(System.Net.HttpStatusCode.OK, result.Status);
        string cookieName = path.Contains("/admin/", StringComparison.Ordinal)
            ? "ts_rt_a"
            : host == "localhost" ? "ts_rt_p" : "ts_rt_t";
        return new Session(result.Str("accessToken"), $"{cookieName}={result.SetCookie(cookieName)}");
    }

    /// <summary>Registers an owner, completes onboarding and returns the new tenant's subdomain.</summary>
    public async Task<OnboardedTenant> OnboardTenantAsync(string businessName = "John & Sons Fashion", string? slugBase = null, Account? owner = null)
    {
        owner ??= await RegisterTailorAsync();
        Session session = await LoginAsync("localhost", owner);

        ApiResult business = await PutAsync("localhost", "/api/onboarding/business", new
        {
            businessName,
            description = "Bespoke tailoring",
            phone = "+2348012345678",
            whatsApp = "+2348012345678",
            email = owner.Email,
            address = "12 Marina Road",
            city = "Lagos",
            state = "Lagos",
            country = "Nigeria"
        }, session.AccessToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, business.Status);
        string draftId = business.Str("draftId");

        ApiResult branding = await PutAsync("localhost", "/api/onboarding/branding",
            new { primaryColor = "#112233", secondaryColor = "#AABBCC" }, session.AccessToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, branding.Status);

        ApiResult complete = await PostAsync("localhost", "/api/onboarding/complete",
            new { draftId, slugBase }, session.AccessToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, complete.Status);

        string subdomain = complete.Str("subdomain");
        Guid tenantId = await TenantIdAsync(subdomain);
        return new OnboardedTenant(owner, subdomain, tenantId, session.AccessToken);
    }

    public async Task<Guid> TenantIdAsync(string subdomain)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Tenants.Where(tenant => tenant.Subdomain == subdomain).Select(tenant => tenant.Id).SingleAsync();
    }

    public async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public Task SetTenantStatusAsync(string subdomain, TenantStatus status) => WithDbAsync(async db =>
    {
        Tenant tenant = await db.Tenants.SingleAsync(candidate => candidate.Subdomain == subdomain);
        tenant.Status = status;
        await db.SaveChangesAsync();
    });

    public async Task<Account> CreateAdminAsync(string? email = null)
    {
        email ??= NewEmail("admin");
        await WithDbAsync(async db =>
        {
            var passwords = Factory.Services.GetRequiredService<IPasswordService>();
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                NormalizedEmail = User.NormalizeEmail(email),
                FirstName = "Plat",
                LastName = "Admin",
                IsPlatformAdmin = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            admin.PasswordHash = passwords.Hash(admin, StrongPassword);
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        });
        return new Account(email, StrongPassword);
    }

    public async Task<Account> RegisterCustomerAsync(string host, string? email = null)
    {
        email ??= NewEmail("customer");
        ApiResult result = await PostAsync(host, "/api/auth/register-customer", RegisterBody(email));
        Assert.Equal(System.Net.HttpStatusCode.Created, result.Status);
        return new Account(email, StrongPassword);
    }

    public void Dispose() => _client.Dispose();
}
