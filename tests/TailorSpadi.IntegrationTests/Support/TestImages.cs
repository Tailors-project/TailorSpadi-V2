using SkiaSharp;

namespace TailorSpadi.IntegrationTests.Support;

public static class TestImages
{
    public static byte[] Png(int width = 64, int height = 64) => Encode(width, height, SKEncodedImageFormat.Png);

    public static byte[] Jpeg(int width = 64, int height = 64) => Encode(width, height, SKEncodedImageFormat.Jpeg);

    public static byte[] Webp(int width = 64, int height = 64) => Encode(width, height, SKEncodedImageFormat.Webp);

    private static byte[] Encode(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.Teal);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(format, 90);
        return data.ToArray();
    }
}
