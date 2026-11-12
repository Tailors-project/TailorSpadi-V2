using Mediator;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;

namespace TailorSpadi.Application.Features.Tenants;

public sealed record GetCurrentTenantQuery : IQuery<CurrentTenantResponse>;

public sealed class GetCurrentTenantHandler(ITenantContext tenantContext)
    : IQueryHandler<GetCurrentTenantQuery, CurrentTenantResponse>
{
    public ValueTask<CurrentTenantResponse> Handle(GetCurrentTenantQuery query, CancellationToken cancellationToken)
    {
        if (tenantContext.Tenant is not { } tenant)
        {
            throw ProblemException.NotFound("tenant_not_found", "This store was not found.");
        }

        return ValueTask.FromResult(new CurrentTenantResponse(
            tenant.Name,
            tenant.Description,
            tenant.Subdomain,
            LogoUrls.For(tenant),
            tenant.PrimaryColor,
            tenant.SecondaryColor,
            tenant.Phone,
            tenant.WhatsApp,
            tenant.Email,
            tenant.Address,
            tenant.City,
            tenant.State,
            tenant.Country));
    }
}
