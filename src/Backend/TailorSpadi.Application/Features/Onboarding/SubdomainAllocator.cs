using Microsoft.EntityFrameworkCore;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Features.Onboarding;

/// <summary>Generates and checks business-name addresses. The unique index prevents concurrent duplicates.</summary>
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
            throw ProblemException.BadRequest("reserved_name", "That business name produces a reserved address. Use a more distinctive business name.");
        }

        return slugBase;
    }

    public async Task EnsureAvailableAsync(string subdomain, Guid draftId, CancellationToken cancellationToken)
    {
        bool taken = await db.Tenants.AsNoTracking()
            .AnyAsync(tenant => tenant.Subdomain == subdomain && tenant.Id != draftId, cancellationToken);
        if (taken)
        {
            throw ProblemException.Conflict("subdomain_taken",
                "That business address is already in use. Go back to Business and use a more distinctive business name.");
        }
    }
}
