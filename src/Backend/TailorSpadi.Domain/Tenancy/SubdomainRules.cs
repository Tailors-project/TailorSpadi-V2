using System.Globalization;
using System.Text;

namespace TailorSpadi.Domain.Tenancy;

/// <summary>Rules for turning a business name into a DNS-safe subdomain label.</summary>
public static class SubdomainRules
{
    public const int MaxLabelLength = 63;
    public const int SuffixDigits = 3;
    public const int MaxSuffix = 999;
    public const int MinBaseLength = 3;

    /// <summary>Leaves room for a hyphen and the numeric suffix inside the 63-character DNS label.</summary>
    public const int MaxBaseLength = MaxLabelLength - 1 - SuffixDigits;

    /// <summary>
    /// Builds a lowercase ASCII slug base: Unicode normalization, diacritic removal, "&amp;" to "and",
    /// separators collapsed to single hyphens, trimmed and truncated. Returns an empty string when
    /// nothing meaningful remains (for example a name written entirely in a non-Latin script).
    /// </summary>
    public static string CreateBase(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string decomposed = text.Replace("&", " and ", StringComparison.Ordinal).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        bool pendingHyphen = false;

        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            char lower = char.ToLowerInvariant(character);
            if (lower is >= 'a' and <= 'z' || lower is >= '0' and <= '9')
            {
                if (pendingHyphen && builder.Length > 0)
                {
                    builder.Append('-');
                }

                pendingHyphen = false;
                builder.Append(lower);
            }
            else
            {
                pendingHyphen = true;
            }
        }

        string slug = builder.ToString();
        if (slug.Length > MaxBaseLength)
        {
            slug = slug[..MaxBaseLength].TrimEnd('-');
        }

        return slug;
    }

    public static bool IsMeaningful(string slugBase) => slugBase.Length >= MinBaseLength;

    public static string WithSuffix(string slugBase, int suffix) =>
        string.Create(CultureInfo.InvariantCulture, $"{slugBase}-{suffix:D3}");

    /// <summary>Extracts the numeric suffix of <paramref name="subdomain"/> when it is exactly base-NNN.</summary>
    public static bool TryGetSuffix(string subdomain, string slugBase, out int suffix)
    {
        suffix = 0;
        string prefix = slugBase + "-";
        if (!subdomain.StartsWith(prefix, StringComparison.Ordinal) || subdomain.Length != prefix.Length + SuffixDigits)
        {
            return false;
        }

        return int.TryParse(subdomain.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out suffix);
    }

    /// <summary>A valid single DNS label: lowercase letters, digits and inner hyphens, at most 63 characters.</summary>
    public static bool IsValidLabel(string? label)
    {
        if (string.IsNullOrEmpty(label) || label.Length > MaxLabelLength)
        {
            return false;
        }

        if (label[0] == '-' || label[^1] == '-')
        {
            return false;
        }

        foreach (char character in label)
        {
            bool allowed = character is >= 'a' and <= 'z' || character is >= '0' and <= '9' || character == '-';
            if (!allowed)
            {
                return false;
            }
        }

        // "xn--" labels are punycode; do not accept look-alike internationalized names.
        return !label.StartsWith("xn--", StringComparison.Ordinal) && !label.Contains("--", StringComparison.Ordinal);
    }
}
