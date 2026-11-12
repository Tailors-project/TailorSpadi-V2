namespace TailorSpadi.Application.Options;

public sealed class TenancyOptions
{
    public const string SectionName = "Tenancy";

    /// <summary>The platform domain. Tenant hosts are single labels directly beneath it.</summary>
    public string BaseDomain { get; set; } = "localhost";

    /// <summary>Hostnames (without port) that identify the platform itself rather than a tenant.</summary>
    public string[] PlatformHosts { get; set; } = [];

    public string PublicScheme { get; set; } = "http";
    public int? PublicPort { get; set; }
}
