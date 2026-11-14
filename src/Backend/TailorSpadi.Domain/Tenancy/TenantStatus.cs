namespace TailorSpadi.Domain.Tenancy;

public enum TenantStatus
{
    /// <summary>The owner is still completing onboarding. The tenant host does not resolve.</summary>
    Onboarding = 0,
    Active = 1,
    Suspended = 2,
    Deactivated = 3
}
