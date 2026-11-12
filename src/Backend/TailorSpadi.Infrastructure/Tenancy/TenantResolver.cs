using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Options;
using TailorSpadi.Application.Tenancy;
using TailorSpadi.Domain.Tenancy;
using TailorSpadi.Infrastructure.Persistence;

namespace TailorSpadi.Infrastructure.Tenancy;

/// <summary>
/// Resolves the tenant from the accepted hostname only. Results (including "not found") are cached briefly in a
/// size-bounded cache so hostnames chosen by an attacker cannot grow memory without limit.
/// </summary>
public sealed class TenantResolver : ITenantResolver, ITenantCache, IDisposable
{
    private static readonly TimeSpan PositiveLifetime = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan NegativeLifetime = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopes;
    private readonly IOptions<TenancyOptions> _options;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 5000 });

    public TenantResolver(IServiceScopeFactory scopes, IOptions<TenancyOptions> options)
    {
        _scopes = scopes;
        _options = options;
    }

    public async Task<TenantResolution> ResolveAsync(string? host, CancellationToken cancellationToken)
    {
        HostClassification classification = HostClassifier.Classify(host, _options.Value);
        if (classification.Kind == HostKind.Platform)
        {
            return TenantResolution.Platform;
        }

        if (classification.Kind == HostKind.Invalid)
        {
            return TenantResolution.Invalid;
        }

        string label = classification.Label!;
        if (_cache.TryGetValue(CacheKey(label), out TenantResolution? cached) && cached is not null)
        {
            return cached;
        }

        // A separate scope has its own, unresolved tenant context; the tenants table is not tenant-filtered.
        using IServiceScope scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Tenant? tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Subdomain == label, cancellationToken);

        TenantResolution result = tenant switch
        {
            null => TenantResolution.NotFound,
            { Status: TenantStatus.Active } active => TenantResolution.Resolved(ToInfo(active, label)),
            { Status: TenantStatus.Suspended or TenantStatus.Deactivated } => TenantResolution.Suspended,
            _ => TenantResolution.NotFound
        };

        _cache.Set(
            CacheKey(label),
            result,
            new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = result.Status == TenantResolutionStatus.Resolved ? PositiveLifetime : NegativeLifetime
            });
        return result;
    }

    public void Invalidate(string subdomain) => _cache.Remove(CacheKey(subdomain));

    public void Dispose() => _cache.Dispose();

    private static string CacheKey(string label) => "tenant:" + label;

    private static TenantInfo ToInfo(Tenant tenant, string subdomain) => new(
        tenant.Id,
        tenant.BusinessName,
        tenant.Description,
        subdomain,
        tenant.LogoStorageKey,
        tenant.PrimaryColor,
        tenant.SecondaryColor,
        tenant.Phone,
        tenant.WhatsApp,
        tenant.Email,
        tenant.Address,
        tenant.City,
        tenant.State,
        tenant.Country);
}
