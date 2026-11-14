using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Abstractions;

/// <summary>The caller described by the validated access token of the current request.</summary>
public interface ICurrentUser
{
    string? UserId { get; }
    Guid? Id { get; }
    bool IsAuthenticated { get; }
    AuthScope? Scope { get; }

    /// <summary>The tenant the session is bound to; null for platform and admin sessions.</summary>
    Guid? TenantId { get; }

    TenantRole? Role { get; }
}
