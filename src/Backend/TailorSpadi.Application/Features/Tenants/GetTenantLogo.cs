using Mediator;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;

namespace TailorSpadi.Application.Features.Tenants;

public sealed record GetTenantLogoQuery : IQuery<LogoFile>;

/// <summary>Public logo of the resolved active tenant. Logos are always stored re-encoded as PNG.</summary>
public sealed class GetTenantLogoHandler(ITenantContext tenantContext, IFileStorage storage)
    : IQueryHandler<GetTenantLogoQuery, LogoFile>
{
    public async ValueTask<LogoFile> Handle(GetTenantLogoQuery query, CancellationToken cancellationToken)
    {
        if (tenantContext.Tenant?.LogoStorageKey is not { } key)
        {
            throw ProblemException.NotFound("not_found", "No logo.");
        }

        byte[] content = await storage.ReadAsync(key, cancellationToken)
            ?? throw ProblemException.NotFound("not_found", "No logo.");
        return new LogoFile(content, "image/png");
    }
}
