namespace TailorSpadi.Application.Security;

public enum SniffedImageFormat
{
    Png = 1,
    Jpeg = 2,
    Webp = 3
}

/// <summary>Identifies the allowed logo formats from their leading bytes. Names and content types are ignored.</summary>
public static class ImageSniffer
{
    public static SniffedImageFormat? Detect(ReadOnlySpan<byte> content)
    {
        ReadOnlySpan<byte> png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (content.Length >= png.Length && content[..png.Length].SequenceEqual(png))
        {
            return SniffedImageFormat.Png;
        }

        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            return SniffedImageFormat.Jpeg;
        }

        // WebP: "RIFF" + 4 size bytes + "WEBP".
        if (content.Length >= 12
            && content[..4].SequenceEqual("RIFF"u8)
            && content.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return SniffedImageFormat.Webp;
        }

        return null;
    }
}
