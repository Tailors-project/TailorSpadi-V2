using System.Globalization;
using System.Text;

namespace TailorSpadi.Domain.Tenancy;

/// <summary>Rules for turning a business name into a DNS-safe subdomain label.</summary>
public static class SubdomainRules
{
    public const int MaxLabelLength = 63;
    public const int MinBaseLength = 3;

    /// <summary>A generated address fits inside a single DNS label.</summary>
    public const int MaxBaseLength = MaxLabelLength;

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
