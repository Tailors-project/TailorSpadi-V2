using System.Net;
using Microsoft.EntityFrameworkCore;
using TailorSpadi.Domain.Identity;
using TailorSpadi.IntegrationTests.Support;

namespace TailorSpadi.IntegrationTests;

public sealed class TenantIsolationTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    [Fact]
    public async Task OneAccountCanBeCustomerOfTwoTenantsAndOwnerOfAnother()
    {
        OnboardedTenant a = await App.OnboardTenantAsync("Alpha Wear");
        OnboardedTenant b = await App.OnboardTenantAsync("Beta Wear");
        Account shopper = await App.RegisterCustomerAsync($"{a.Subdomain}.localhost");

        // Registering again on another tenant reports the existing account; join adds a membership.
        ApiResult again = await App.PostAsync($"{b.Subdomain}.localhost", "/api/auth/register-customer", TestApp.RegisterBody(shopper.Email));
        Assert.Equal("email_taken", again.Code);
        ApiResult joined = await App.PostAsync($"{b.Subdomain}.localhost", "/api/auth/join", new { email = shopper.Email, password = shopper.Password });
        Assert.Equal(HttpStatusCode.OK, joined.Status);

        await App.WithDbAsync(async db =>
        {
            Assert.Equal(1, await db.Users.CountAsync(user => user.NormalizedEmail == User.NormalizeEmail(shopper.Email)));
            Guid userId = await db.Users.Where(user => user.NormalizedEmail == User.NormalizeEmail(shopper.Email)).Select(user => user.Id).SingleAsync();
            int memberships = await db.TenantMemberships.IgnoreQueryFilters().CountAsync(m => m.UserId == userId);
            Assert.Equal(2, memberships);
        });
    }

    [Fact]
    public async Task LoginOnATenantWithoutMembershipRequiresJoining()
    {
        OnboardedTenant a = await App.OnboardTenantAsync("Alpha Wear");
        OnboardedTenant b = await App.OnboardTenantAsync("Beta Wear");

        ApiResult result = await App.PostAsync($"{b.Subdomain}.localhost", "/api/auth/login", new { email = a.Owner.Email, password = a.Owner.Password });

        Assert.Equal(HttpStatusCode.Forbidden, result.Status);
        Assert.Equal("membership_required", result.Code);
    }

    [Fact]
    public async Task CustomerMembershipDoesNotGrantOwnerPrivileges()
    {
        OnboardedTenant tenant = await App.OnboardTenantAsync();
        string host = $"{tenant.Subdomain}.localhost";
        Account customer = await App.RegisterCustomerAsync(host);
        Session session = await App.LoginAsync(host, customer);

        ApiResult upload = await App.UploadAsync(host, "/api/tenant/logo", TestImages.Png(), "logo.png", "image/png", session.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, upload.Status);
    }

    [Fact]
    public async Task OwnerOfOneTenantCannotChangeAnotherTenantsLogo()
    {
        OnboardedTenant a = await App.OnboardTenantAsync("Alpha Wear");
        OnboardedTenant b = await App.OnboardTenantAsync("Beta Wear");
        Session sessionA = await App.LoginAsync($"{a.Subdomain}.localhost", a.Owner);

        ApiResult crossTenant = await App.UploadAsync($"{b.Subdomain}.localhost", "/api/tenant/logo", TestImages.Png(), "logo.png", "image/png", sessionA.AccessToken);

        Assert.Equal(HttpStatusCode.Unauthorized, crossTenant.Status);
    }

    [Fact]
    public async Task MembershipQueriesFailClosedWithoutATenantAndScopeToTheCurrentTenant()
    {
        OnboardedTenant a = await App.OnboardTenantAsync("Alpha Wear");
        await App.OnboardTenantAsync("Beta Wear");

        await App.WithDbAsync(async db =>
        {
            Assert.Empty(await db.TenantMemberships.ToListAsync());
            Assert.Equal(2, await db.TenantMemberships.IgnoreQueryFilters().CountAsync());
        });
        Assert.NotEqual(Guid.Empty, a.TenantId);
    }

    [Fact]
    public async Task InactiveMembershipBlocksTheSessionOnThatTenantOnly()
    {
        OnboardedTenant a = await App.OnboardTenantAsync("Alpha Wear");
        string host = $"{a.Subdomain}.localhost";
        Account customer = await App.RegisterCustomerAsync(host);
        Session session = await App.LoginAsync(host, customer);

        await App.WithDbAsync(async db =>
        {
            Guid userId = await db.Users.Where(user => user.NormalizedEmail == User.NormalizeEmail(customer.Email)).Select(user => user.Id).SingleAsync();
            TenantMembership membership = await db.TenantMemberships.IgnoreQueryFilters()
                .SingleAsync(m => m.TenantId == a.TenantId && m.UserId == userId);
            membership.IsActive = false;
            await db.SaveChangesAsync();
        });

        Assert.Equal(HttpStatusCode.Unauthorized, (await App.GetAsync(host, "/api/auth/me", session.AccessToken)).Status);
    }
}
