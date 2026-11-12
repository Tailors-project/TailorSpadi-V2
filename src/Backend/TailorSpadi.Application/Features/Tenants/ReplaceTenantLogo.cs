using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Features.Onboarding;
using TailorSpadi.Application.Options;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Features.Tenants;

public sealed record ReplaceTenantLogoCommand(byte[] Content) : ICommand<LogoUrlResponse>;

public sealed class ReplaceTenantLogoValidator : AbstractValidator<ReplaceTenantLogoCommand>
{
    public ReplaceTenantLogoValidator(IOptions<StorageOptions> options)
    {
        RuleFor(command => command.Content).NotNull().Must(content => content.Length > 0)
            .WithMessage("Choose an image file.")
            .Must(content => content.Length <= options.Value.MaxLogoBytes)
            .WithMessage("The image is too large.");
    }
}

/// <summary>An owner holding ManageBusiness replaces the logo of the current tenant only.</summary>
public sealed class ReplaceTenantLogoHandler(
    IAppDbContext db, ICurrentUser currentUser, ITenantContext tenantContext, LogoService logos, ITenantCache cache)
    : ICommandHandler<ReplaceTenantLogoCommand, LogoUrlResponse>
{
    public async ValueTask<LogoUrlResponse> Handle(ReplaceTenantLogoCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Scope != AuthScope.Tenant
            || currentUser.Role is not { } role
            || !Permissions.Has(role, Permissions.ManageBusiness)
            || tenantContext.Tenant is not { } info
            || currentUser.TenantId != info.Id)
        {
            throw ProblemException.Forbidden("forbidden", "You are not allowed to do that.");
        }

        Tenant tenant = await db.Tenants.FirstOrDefaultAsync(candidate => candidate.Id == info.Id, cancellationToken)
            ?? throw ProblemException.NotFound("tenant_not_found", "This store was not found.");

        await logos.ReplaceAsync(tenant, command.Content, cancellationToken);
        cache.Invalidate(info.Subdomain);

        string version = Path.GetFileNameWithoutExtension(tenant.LogoStorageKey!);
        return new LogoUrlResponse("/api/tenant/current/logo?v=" + Uri.EscapeDataString(version));
    }
}
