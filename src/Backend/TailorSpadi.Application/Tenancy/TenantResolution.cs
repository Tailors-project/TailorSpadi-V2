namespace TailorSpadi.Application.Tenancy;

public enum TenantResolutionStatus
{
    /// <summary>A platform host: no tenant.</summary>
    Platform = 0,

    /// <summary>A valid tenant subdomain of an active tenant.</summary>
    Resolved = 1,

    /// <summary>Unknown or incomplete tenant.</summary>
    NotFound = 2,

    /// <summary>Suspended or deactivated tenant.</summary>
    Suspended = 3,

    /// <summary>A host that is neither a platform host nor a well-formed tenant host.</summary>
    Invalid = 4
}

public sealed record TenantResolution(TenantResolutionStatus Status, TenantInfo? Tenant = null)
{
    public static TenantResolution Platform { get; } = new(TenantResolutionStatus.Platform);
    public static TenantResolution NotFound { get; } = new(TenantResolutionStatus.NotFound);
    public static TenantResolution Suspended { get; } = new(TenantResolutionStatus.Suspended);
    public static TenantResolution Invalid { get; } = new(TenantResolutionStatus.Invalid);

    public static TenantResolution Resolved(TenantInfo tenant) => new(TenantResolutionStatus.Resolved, tenant);
}
