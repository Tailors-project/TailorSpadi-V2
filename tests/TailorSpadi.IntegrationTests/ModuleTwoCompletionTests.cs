using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Features.Onboarding;
using TailorSpadi.Application.Features.Tenants;
using TailorSpadi.Application.Tenancy;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;
using TailorSpadi.Infrastructure.Persistence;
using TailorSpadi.IntegrationTests.Support;

namespace TailorSpadi.IntegrationTests;

public sealed class ModuleTwoCompletionTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    [Fact]
    public async Task SimultaneousRefreshRevokesTheReusedFamily()
    {
        Account account = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", account);
        ApiResult[] responses = await Task.WhenAll(
            App.PostAsync("localhost", "/api/auth/refresh", cookie: session.RefreshCookie, cookieGuard: true),
            App.PostAsync("localhost", "/api/auth/refresh", cookie: session.RefreshCookie, cookieGuard: true));
        ApiResult winner = Assert.Single(responses, result => result.Status == HttpStatusCode.OK);
        Assert.Single(responses, result => result.Status == HttpStatusCode.Unauthorized);
        ApiResult retry = await App.PostAsync("localhost", "/api/auth/refresh",
            cookie: "ts_rt_p=" + winner.SetCookie("ts_rt_p"), cookieGuard: true);
        Assert.Equal(HttpStatusCode.Unauthorized, retry.Status);
    }

    [Fact]
    public async Task FailedLogoDatabaseSaveRemovesTheNewFile()
    {
        await App.WithDbAsync(async db =>
        {
            var tenant = new Tenant { Id = Guid.NewGuid(), CreatedByUserId = Guid.NewGuid() };
            db.Tenants.Add(tenant);
            var service = new LogoService(Factory.Services.GetRequiredService<IImageProcessor>(),
                Factory.Services.GetRequiredService<IFileStorage>(), db, TimeProvider.System);
            await Assert.ThrowsAsync<DbUpdateException>(() => service.ReplaceAsync(tenant, TestImages.Png(), default));
            Assert.Empty(Directory.EnumerateFiles(Factory.StorageRoot, "*", SearchOption.AllDirectories));
        });
    }

    [Fact]
    public async Task RefreshRejectsAChangedSecurityStamp()
    {
        Account account = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", account);
        await App.WithDbAsync(async db =>
        {
            User user = await db.Users.SingleAsync();
            user.RotateSecurityStamp();
            await db.SaveChangesAsync();
        });
        ApiResult response = await App.PostAsync("localhost", "/api/auth/refresh", cookie: session.RefreshCookie, cookieGuard: true);
        Assert.Equal(HttpStatusCode.Unauthorized, response.Status);
    }

    [Fact]
    public async Task SavedAddressAndReviewStepSurviveReload()
    {
        Account account = await App.RegisterTailorAsync();
        Session session = await App.LoginAsync("localhost", account);
        await App.PutAsync("localhost", "/api/onboarding/business", new
        {
            businessName = "Ada Fashion",
            phone = "+2348012345678",
            email = account.Email,
            country = "Nigeria"
        }, session.AccessToken);
        await App.PutAsync("localhost", "/api/onboarding/branding", new
        {
            primaryColor = "#112233",
            secondaryColor = "#AABBCC"
        }, session.AccessToken);
        ApiResult saved = await App.PutAsync("localhost", "/api/onboarding/subdomain", new { slugBase = "ada-bespoke" }, session.AccessToken);
        Assert.Equal(HttpStatusCode.OK, saved.Status);
        ApiResult resumed = await App.GetAsync("localhost", "/api/onboarding", session.AccessToken);
        Assert.Equal(4, resumed.Json.GetProperty("step").GetInt32());
        Assert.Equal("ada-bespoke", resumed.Str("slugBase"));
        Assert.Equal(HttpStatusCode.NotFound, (await App.GetAsync("ada-bespoke-001.localhost", "/api/tenant/current")).Status);
        ApiResult complete = await App.PostAsync("localhost", "/api/onboarding/complete",
            new { draftId = resumed.Str("draftId") }, session.AccessToken);
        Assert.StartsWith("ada-bespoke-", complete.Str("subdomain"));
        Assert.Equal(HttpStatusCode.OK, (await App.GetAsync(complete.Str("subdomain") + ".localhost", "/api/tenant/current")).Status);
    }

    [Fact]
    public async Task TenantStatusCommandRequiresAdminAndRevokesSessionsAndCache()
    {
        OnboardedTenant tenant = await App.OnboardTenantAsync();
        string host = tenant.Subdomain + ".localhost";
        Session session = await App.LoginAsync(host, tenant.Owner);
        await App.GetAsync(host, "/api/tenant/current");
        Account admin = await App.CreateAdminAsync();
        using IServiceScope scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Guid adminId = await db.Users.Where(u => u.NormalizedEmail == User.NormalizeEmail(admin.Email)).Select(u => u.Id).SingleAsync();
        var caller = new TestCurrentUser(adminId, AuthScope.Platform);
        var handler = new SetTenantStatusHandler(db, caller, scope.ServiceProvider.GetRequiredService<ITenantCache>(), TimeProvider.System);
        await Assert.ThrowsAsync<ProblemException>(async () => await handler.Handle(new(tenant.TenantId, TenantStatus.Suspended), default));
        caller.Scope = AuthScope.Admin;
        await handler.Handle(new(tenant.TenantId, TenantStatus.Suspended), default);
        Assert.Equal(HttpStatusCode.Forbidden, (await App.GetAsync(host, "/api/tenant/current")).Status);
        await handler.Handle(new(tenant.TenantId, TenantStatus.Active), default);
        Assert.Equal(HttpStatusCode.OK, (await App.GetAsync(host, "/api/tenant/current")).Status);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await App.PostAsync(host, "/api/auth/refresh", cookie: session.RefreshCookie, cookieGuard: true)).Status);
    }

    [Fact]
    public async Task CrossTenantWritesAndAssignedSubdomainChangesAreRejected()
    {
        OnboardedTenant first = await App.OnboardTenantAsync("First Fashion");
        OnboardedTenant second = await App.OnboardTenantAsync("Second Fashion");
        using IServiceScope scope = Factory.Services.CreateScope();
        var context = (TenantContext)scope.ServiceProvider.GetRequiredService<ITenantContext>();
        TenantResolution resolved = await scope.ServiceProvider.GetRequiredService<ITenantResolver>()
            .ResolveAsync(first.Subdomain + ".localhost", default);
        context.Set(resolved.Tenant);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        TenantMembership membership = await db.TenantMemberships.IgnoreQueryFilters().SingleAsync(m => m.TenantId == second.TenantId);
        membership.IsActive = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        Tenant tenant = await db.Tenants.SingleAsync(t => t.Id == first.TenantId);
        tenant.Subdomain = "changed-001";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DatabaseRejectsDuplicateMembershipAndMissingUser()
    {
        OnboardedTenant tenant = await App.OnboardTenantAsync();
        await App.WithDbAsync(async db =>
        {
            TenantMembership owner = await db.TenantMemberships.IgnoreQueryFilters().AsNoTracking().SingleAsync();
            db.TenantMemberships.Add(new TenantMembership { Id = Guid.NewGuid(), TenantId = tenant.TenantId, UserId = owner.UserId });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            db.TenantMemberships.Add(new TenantMembership { Id = Guid.NewGuid(), TenantId = tenant.TenantId, UserId = Guid.NewGuid() });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            var outsider = new User { Id = Guid.NewGuid(), Email = "outsider@example.test", NormalizedEmail = "OUTSIDER@EXAMPLE.TEST", CreatedAtUtc = DateTime.UtcNow };
            db.Users.Add(outsider);
            await db.SaveChangesAsync();
            db.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = outsider.Id,
                TenantId = tenant.TenantId,
                Scope = AuthScope.Tenant,
                FamilyId = Guid.NewGuid(),
                TokenHash = new string('a', 64),
                SecurityStamp = outsider.SecurityStamp,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        });
    }

    private sealed class TestCurrentUser(Guid id, AuthScope scope) : ICurrentUser
    {
        public string? UserId => id.ToString();
        public Guid? Id => id;
        public bool IsAuthenticated => true;
        public AuthScope? Scope { get; set; } = scope;
        public Guid? TenantId => null;
        public TenantRole? Role => null;
    }
}
