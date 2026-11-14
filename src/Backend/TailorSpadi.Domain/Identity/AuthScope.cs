namespace TailorSpadi.Domain.Identity;

/// <summary>What a session is allowed to do, and on which kind of host it is valid.</summary>
public enum AuthScope
{
    /// <summary>A session on one tenant's host, bound to that tenant.</summary>
    Tenant = 0,

    /// <summary>A session on the platform host for onboarding and listing the user's businesses.</summary>
    Platform = 1,

    /// <summary>A TailorSpadi platform administrator session.</summary>
    Admin = 2
}
