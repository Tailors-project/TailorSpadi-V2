using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Features.Identity;
using TailorSpadi.Application.Options;
using TailorSpadi.Application.Tenancy;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Security;

/// <summary>Authentication, session creation and revocation shared by the identity use cases.</summary>
public sealed class SessionService(
    IAppDbContext db,
    ITenantContext tenantContext,
    IPasswordService passwords,
    ITokenService tokens,
    IPublicUrlBuilder urls,
    IOptions<AuthOptions> authOptions,
    TimeProvider clock)
{
    private AuthOptions Options => authOptions.Value;

    public DateTime UtcNow => clock.GetUtcNow().UtcDateTime;

    public static ProblemException InvalidCredentials() =>
        ProblemException.Unauthorized("invalid_credentials", "The email or password is incorrect.");

    /// <summary>
    /// Verifies email and password for the given scope. Every failure (unknown account, wrong password,
    /// lockout, inactive or wrong account type) returns the same error so accounts cannot be enumerated.
    /// </summary>
    public async Task<User> AuthenticateAsync(
        string email, string password, AuthScope scope, CancellationToken cancellationToken)
    {
        DateTime now = UtcNow;
        string normalized = User.NormalizeEmail(email);
        User? user = await db.Users.FirstOrDefaultAsync(candidate => candidate.NormalizedEmail == normalized, cancellationToken);

        if (user is null)
        {
            passwords.BurnTime(password);
            db.Audit(clock, "login.failed", null, tenantContext.TenantId);
            await db.SaveChangesAsync(cancellationToken);
            throw InvalidCredentials();
        }

        if (user.IsLockedOut(now))
        {
            passwords.BurnTime(password);
            db.Audit(clock, "login.locked", user.Id, tenantContext.TenantId);
            await db.SaveChangesAsync(cancellationToken);
            throw InvalidCredentials();
        }

        PasswordVerification verification = passwords.Verify(user, user.PasswordHash, password);
        if (verification == PasswordVerification.Failed)
        {
            user.RegisterFailedLogin(now, Options.MaxFailedLogins, TimeSpan.FromMinutes(Options.LockoutMinutes));
            db.Audit(clock, "login.failed", user.Id, tenantContext.TenantId);
            await db.SaveChangesAsync(cancellationToken);
            throw InvalidCredentials();
        }

        bool wrongAccountType = (scope == AuthScope.Admin) != user.IsPlatformAdmin;
        if (!user.IsActive || wrongAccountType)
        {
            db.Audit(clock, "login.rejected", user.Id, tenantContext.TenantId, user.IsActive ? "account-type" : "inactive");
            await db.SaveChangesAsync(cancellationToken);
            throw InvalidCredentials();
        }

        if (verification == PasswordVerification.SuccessRehashNeeded)
        {
            user.PasswordHash = passwords.Hash(user, password);
        }

        user.RegisterSuccessfulLogin(now);
        db.Audit(clock, "credentials.verified", user.Id, tenantContext.TenantId, scope.ToString());
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    /// <summary>Creates a refresh token row (not yet saved) and returns the raw token for the cookie.</summary>
    public (RefreshToken Entity, string RawToken) CreateRefreshToken(
        User user, AuthScope scope, Guid? tenantId, Guid familyId, string userAgent)
    {
        string raw = tokens.GenerateOpaqueToken();
        DateTime now = UtcNow;
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = scope == AuthScope.Tenant ? tenantId : null,
            Scope = scope,
            FamilyId = familyId,
            TokenHash = tokens.HashToken(raw),
            SecurityStamp = user.SecurityStamp,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(Options.RefreshTokenDays),
            UserAgent = userAgent.Length > 200 ? userAgent[..200] : userAgent
        };
        db.RefreshTokens.Add(entity);
        return (entity, raw);
    }

    /// <summary>Starts a new session: a new refresh-token family plus an access token.</summary>
    public async Task<SessionResult> StartAsync(
        User user, AuthScope scope, TenantMembership? membership, string userAgent, CancellationToken cancellationToken)
    {
        Guid? tenantId = scope == AuthScope.Tenant ? membership?.TenantId : null;
        (RefreshToken entity, string raw) = CreateRefreshToken(user, scope, tenantId, Guid.NewGuid(), userAgent);
        SessionResponse response = await IssueAccessAsync(user, scope, membership, cancellationToken);
        db.Audit(clock, "login.succeeded", user.Id, tenantId, scope.ToString());
        await db.SaveChangesAsync(cancellationToken);
        return new SessionResult(response, scope, raw, entity.ExpiresAtUtc);
    }

    public async Task<SessionResponse> IssueAccessAsync(
        User user, AuthScope scope, TenantMembership? membership, CancellationToken cancellationToken)
    {
        string? role = scope switch
        {
            AuthScope.Tenant => membership?.Role.ToString(),
            AuthScope.Admin => "PlatformAdmin",
            _ => null
        };

        AccessToken token = tokens.CreateAccessToken(new AccessTokenRequest(
            user.Id, scope, scope == AuthScope.Tenant ? membership?.TenantId : null, role, user.SecurityStamp));
        SessionInfo info = await BuildInfoAsync(user, scope, membership, cancellationToken);
        return new SessionResponse(token.Token, token.ExpiresInSeconds, info);
    }

    public async Task<SessionInfo> BuildInfoAsync(
        User user, AuthScope scope, TenantMembership? membership, CancellationToken cancellationToken)
    {
        var summary = new UserSummary(
            user.Id, user.Email, user.FirstName, user.LastName, string.IsNullOrWhiteSpace(user.Phone) ? null : user.Phone);

        if (scope == AuthScope.Admin)
        {
            return new SessionInfo("admin", summary, "PlatformAdmin", null, [], false);
        }

        if (scope == AuthScope.Tenant)
        {
            TenantInfo? tenant = tenantContext.Tenant;
            TenantSummary? tenantSummary = tenant is null
                ? null
                : new TenantSummary(tenant.Name, tenant.Subdomain, urls.Tenant(tenant.Subdomain, "/"));
            return new SessionInfo("tenant", summary, membership?.Role.ToString(), tenantSummary, [], false);
        }

        // Platform scope lists the user's businesses across tenants, so it reads without the tenant filter
        // and restricts itself to this user's own memberships.
        var rows = await (
            from m in db.TenantMemberships.IgnoreQueryFilters().AsNoTracking()
            join t in db.Tenants.AsNoTracking() on m.TenantId equals t.Id
            where m.UserId == user.Id && m.IsActive && m.Role != TenantRole.Customer && t.Status == TenantStatus.Active
            orderby t.BusinessName
            select new { t.BusinessName, t.Subdomain, m.Role }).ToListAsync(cancellationToken);

        List<BusinessSummary> businesses = rows
            .Where(row => row.Subdomain is not null)
            .Select(row => new BusinessSummary(
                row.BusinessName,
                row.Subdomain!,
                row.Role.ToString(),
                urls.Tenant(row.Subdomain!, "/"),
                urls.Tenant(row.Subdomain!, "/login")))
            .ToList();

        bool hasDraft = await db.Tenants.AsNoTracking().AnyAsync(t => t.DraftOwnerUserId == user.Id, cancellationToken);
        return new SessionInfo("platform", summary, null, null, businesses, hasDraft);
    }

    /// <summary>Revokes every refresh token of the user, optionally keeping one session family alive.</summary>
    public async Task RevokeSessionsAsync(Guid userId, Guid? exceptFamilyId, CancellationToken cancellationToken)
    {
        DateTime now = UtcNow;
        List<RefreshToken> active = await db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null
                && (exceptFamilyId == null || token.FamilyId != exceptFamilyId))
            .ToListAsync(cancellationToken);
        foreach (RefreshToken token in active)
        {
            token.Revoke(now);
        }
    }

    /// <summary>Revokes every token of one session family (a logout, or a detected token replay).</summary>
    public Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken) =>
        db.RevokeRefreshFamilyAsync(familyId, UtcNow, cancellationToken);
}
