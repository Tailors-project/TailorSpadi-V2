using TailorSpadi.Application.Security;

namespace TailorSpadi.UnitTests;

public sealed class ImageSnifferTests
{
    [Fact]
    public void DetectsPng()
    {
        byte[] content = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0];
        Assert.Equal(SniffedImageFormat.Png, ImageSniffer.Detect(content));
    }

    [Fact]
    public void DetectsJpeg()
    {
        byte[] content = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10];
        Assert.Equal(SniffedImageFormat.Jpeg, ImageSniffer.Detect(content));
    }

    [Fact]
    public void DetectsWebp()
    {
        byte[] content = [.. "RIFF"u8.ToArray(), 1, 2, 3, 4, .. "WEBP"u8.ToArray(), 0, 0];
        Assert.Equal(SniffedImageFormat.Webp, ImageSniffer.Detect(content));
    }

    [Fact]
    public void RejectsRiffThatIsNotWebp()
    {
        byte[] content = [.. "RIFF"u8.ToArray(), 1, 2, 3, 4, .. "WAVE"u8.ToArray(), 0, 0];
        Assert.Null(ImageSniffer.Detect(content));
    }

    [Fact]
    public void RejectsSvgTextGifAndEmpty()
    {
        Assert.Null(ImageSniffer.Detect("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"u8));
        Assert.Null(ImageSniffer.Detect("GIF89a......"u8));
        Assert.Null(ImageSniffer.Detect("just text pretending to be a.png"u8));
        Assert.Null(ImageSniffer.Detect(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void ShortInputsDoNotThrow()
    {
        Assert.Null(ImageSniffer.Detect([0x89, 0x50]));
        Assert.Null(ImageSniffer.Detect([0xFF, 0xD8]));
    }
}
