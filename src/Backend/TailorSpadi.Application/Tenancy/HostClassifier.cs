using TailorSpadi.Application.Options;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Tenancy;

public enum HostKind
{
    Invalid = 0,
    Platform = 1,
    TenantCandidate = 2
}

public sealed record HostClassification(HostKind Kind, string? Label = null);

/// <summary>
/// Classifies an accepted Host header. A tenant candidate is exactly one valid DNS label beneath the base
/// domain. Matching a pattern says nothing about whether such a tenant exists.
/// </summary>
public static class HostClassifier
{
    public static HostClassification Classify(string? hostHeader, TenancyOptions options)
    {
        string? host = StripPort(hostHeader);
        if (host is null)
        {
            return new HostClassification(HostKind.Invalid);
        }

        string baseDomain = options.BaseDomain.Trim().TrimEnd('.').ToLowerInvariant();
        if (host == baseDomain || options.PlatformHosts.Any(platform =>
                string.Equals(platform.Trim().TrimEnd('.'), host, StringComparison.OrdinalIgnoreCase)))
        {
            return new HostClassification(HostKind.Platform);
        }

        string suffix = "." + baseDomain;
        if (host.EndsWith(suffix, StringComparison.Ordinal))
        {
            string label = host[..^suffix.Length];
            return SubdomainRules.IsValidLabel(label)
                ? new HostClassification(HostKind.TenantCandidate, label)
                : new HostClassification(HostKind.Invalid);
        }

        return new HostClassification(HostKind.Invalid);
    }

    private static string? StripPort(string? hostHeader)
    {
        if (string.IsNullOrWhiteSpace(hostHeader))
        {
            return null;
        }

        string value = hostHeader.Trim().ToLowerInvariant();
        if (value.StartsWith('['))
        {
            int end = value.IndexOf(']', StringComparison.Ordinal);
            return end < 0 ? null : value[..(end + 1)];
        }

        int colon = value.LastIndexOf(':');
        if (colon >= 0)
        {
            // Exactly one colon means host:port. More than one is not a valid non-bracketed host.
            if (value.IndexOf(':', StringComparison.Ordinal) != colon)
            {
                return null;
            }

            value = value[..colon];
        }

        value = value.TrimEnd('.');
        return value.Length == 0 ? null : value;
    }
}
