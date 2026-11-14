using TailorSpadi.Application.Options;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.UnitTests;

public sealed class DomainRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void EmailNormalizationIgnoresCaseAndSurroundingSpace()
    {
        Assert.Equal(User.NormalizeEmail("Ada@Example.com"), User.NormalizeEmail("  ada@example.COM "));
    }

    [Fact]
    public void FifthFailureLocksTheAccountForTheLockoutPeriod()
    {
        var user = new User();
        TimeSpan lockout = TimeSpan.FromMinutes(15);

        for (int attempt = 0; attempt < 4; attempt++)
        {
            user.RegisterFailedLogin(Now, 5, lockout);
            Assert.False(user.IsLockedOut(Now));
        }

        user.RegisterFailedLogin(Now, 5, lockout);

        Assert.True(user.IsLockedOut(Now));
        Assert.True(user.IsLockedOut(Now.AddMinutes(14)));
        Assert.False(user.IsLockedOut(Now.AddMinutes(16)));
    }

    [Fact]
    public void SuccessfulLoginClearsFailuresAndLockout()
    {
        var user = new User();
        user.RegisterFailedLogin(Now, 1, TimeSpan.FromMinutes(15));
        Assert.True(user.IsLockedOut(Now));

        user.RegisterSuccessfulLogin(Now.AddHours(1));

        Assert.False(user.IsLockedOut(Now.AddHours(1)));
        Assert.Equal(0, user.FailedLoginCount);
        Assert.Equal(Now.AddHours(1), user.LastLoginAtUtc);
    }

    [Fact]
    public void SecurityStampChangesWhenRotated()
    {
        var user = new User();
        string before = user.SecurityStamp;

        user.RotateSecurityStamp();

        Assert.True(before != user.SecurityStamp);
    }

    [Fact]
    public void RefreshTokenIsUsableUntilRevokedOrExpired()
    {
        var token = new RefreshToken { ExpiresAtUtc = Now.AddDays(1) };
        Assert.True(token.IsUsable(Now));
        Assert.False(token.IsUsable(Now.AddDays(2)));

        token.Revoke(Now);
        Assert.False(token.IsUsable(Now));
    }

    [Fact]
    public void RevokingTwiceKeepsTheFirstRevocationTime()
    {
        var token = new RefreshToken { ExpiresAtUtc = Now.AddDays(1) };
        token.Revoke(Now);
        token.Revoke(Now.AddHours(5));

        Assert.Equal(Now, token.RevokedAtUtc);
    }

    [Fact]
    public void ResetTokenIsSingleUseAndExpires()
    {
        var token = new PasswordResetToken { ExpiresAtUtc = Now.AddMinutes(30) };
        Assert.True(token.IsUsable(Now));
        Assert.False(token.IsUsable(Now.AddMinutes(31)));

        token.UsedAtUtc = Now;
        Assert.False(token.IsUsable(Now));
    }

    [Fact]
    public void ActivatingADraftSetsSubdomainStatusAndClearsTheDraftLink()
    {
        var tenant = new Tenant { DraftOwnerUserId = Guid.NewGuid(), OnboardingStep = 3 };
        Guid stampBefore = tenant.ConcurrencyStamp;

        tenant.Activate("john-fashion-001", Now);

        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.Equal("john-fashion-001", tenant.Subdomain);
        Assert.Null(tenant.DraftOwnerUserId);
        Assert.Equal(Tenant.FinalOnboardingStep, tenant.OnboardingStep);
        Assert.True(stampBefore != tenant.ConcurrencyStamp);
    }

    [Fact]
    public void OnlyADraftCanBeActivated()
    {
        var tenant = new Tenant { Status = TenantStatus.Active, Subdomain = "x-001" };

        Assert.Throws<InvalidOperationException>(() => tenant.Activate("y-001", Now));
    }

    [Fact]
    public void OnboardingProgressOnlyMovesForward()
    {
        var tenant = new Tenant();
        tenant.AdvanceTo(3);
        tenant.AdvanceTo(2);
        Assert.Equal(3, tenant.OnboardingStep);

        tenant.AdvanceTo(99);
        Assert.Equal(Tenant.FinalOnboardingStep, tenant.OnboardingStep);
    }

    [Fact]
    public void OnlyOwnersHaveManageBusiness()
    {
        Assert.True(Permissions.Has(TenantRole.TenantOwner, Permissions.ManageBusiness));
        Assert.False(Permissions.Has(TenantRole.TenantManager, Permissions.ManageBusiness));
        Assert.False(Permissions.Has(TenantRole.TenantStaff, Permissions.ManageBusiness));
        Assert.False(Permissions.Has(TenantRole.Customer, Permissions.ManageBusiness));
    }

    [Fact]
    public void CustomersAreNotBackofficeRoles()
    {
        Assert.False(new TenantMembership { Role = TenantRole.Customer }.IsBackofficeRole);
        Assert.True(new TenantMembership { Role = TenantRole.TenantStaff }.IsBackofficeRole);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("short", false)]
    [InlineData("replace-with-a-random-secret-value-1234567890", false)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]
    [InlineData("this-is-my-password-0123456789-abcdef", false)]
    [InlineData("k8Qz3vN0xLp2Rt7YwB5mHc9JdFa4UeSg", true)]
    public void SigningKeyStrengthIsChecked(string? key, bool acceptable)
    {
        Assert.Equal(acceptable, AuthOptions.IsAcceptableSigningKey(key));
    }
}
