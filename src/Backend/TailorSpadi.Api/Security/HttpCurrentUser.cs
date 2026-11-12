using System.Security.Claims;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Domain.Identity;
using TailorSpadi.Infrastructure.Security;

namespace TailorSpadi.Api.Security;

/// <summary>The caller described by the validated access token. Token claims are only trusted after validation.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? UserId => IsAuthenticated ? Principal!.FindFirstValue("sub") : null;

    public Guid? Id => Guid.TryParse(UserId, out Guid id) ? id : null;

    public AuthScope? Scope => IsAuthenticated
        && Enum.TryParse(Principal!.FindFirstValue(TokenService.ScopeClaim), ignoreCase: true, out AuthScope scope)
        ? scope
        : null;

    public Guid? TenantId => IsAuthenticated && Guid.TryParse(Principal!.FindFirstValue(TokenService.TenantClaim), out Guid id)
        ? id
        : null;

    public TenantRole? Role => IsAuthenticated && Enum.TryParse(Principal!.FindFirstValue(TokenService.RoleClaim), out TenantRole role)
        ? role
        : null;
}
