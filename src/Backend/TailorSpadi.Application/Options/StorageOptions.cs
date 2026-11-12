namespace TailorSpadi.Application.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Local storage root. Keep it outside the web-served source tree.</summary>
    public string Root { get; set; } = "storage";

    public int MaxLogoBytes { get; set; } = 2 * 1024 * 1024;

    /// <summary>Decoded pixel budget, a defence against decompression bombs.</summary>
    public int MaxLogoPixels { get; set; } = 16_000_000;

    /// <summary>The longest edge of the stored logo.</summary>
    public int LogoMaxEdge { get; set; } = 512;
}
