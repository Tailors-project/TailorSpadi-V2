using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Features.Onboarding;

/// <summary>Chooses the slug base and the next free numeric suffix. The unique index is the final arbiter.</summary>
public sealed class SubdomainAllocator(IAppDbContext db)
{
    /// <summary>Validates the requested (or business-name-derived) base and returns it.</summary>
    public static string ResolveBase(string? requested, string businessName)
    {
        string slugBase = SubdomainRules.CreateBase(string.IsNullOrWhiteSpace(requested) ? businessName : requested);
        if (!SubdomainRules.IsMeaningful(slugBase))
        {
            throw ProblemException.BadRequest(
                "invalid_name", "Use at least three letters or numbers (A-Z, 0-9) for your web address.");
        }

        if (ReservedSubdomains.IsReserved(slugBase))
        {
            throw ProblemException.BadRequest("reserved_name", "That name is reserved. Please choose another.");
        }

        return slugBase;
    }

    /// <summary>The next unused suffix for the base, starting at 1.</summary>
    public async Task<int> NextSuffixAsync(string slugBase, CancellationToken cancellationToken)
    {
        string prefix = slugBase + "-";
        List<string?> existing = await db.Tenants.AsNoTracking()
            .Where(tenant => tenant.Subdomain != null && tenant.Subdomain.StartsWith(prefix))
            .Select(tenant => tenant.Subdomain)
            .ToListAsync(cancellationToken);

        int highest = 0;
        foreach (string? subdomain in existing)
        {
            if (subdomain is not null && SubdomainRules.TryGetSuffix(subdomain, slugBase, out int suffix) && suffix > highest)
            {
                highest = suffix;
            }
        }

        int next = highest + 1;
        if (next > SubdomainRules.MaxSuffix)
        {
            throw ProblemException.Conflict(
                "subdomain_exhausted", "Too many businesses use that name. Please choose a different one.");
        }

        return next;
    }
}
