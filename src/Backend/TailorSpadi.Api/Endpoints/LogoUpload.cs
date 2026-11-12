using TailorSpadi.Application.Options;

namespace TailorSpadi.Api.Endpoints;

internal static class LogoUpload
{
    /// <summary>
    /// Reads the multipart "file" part into memory, bounded by the configured size. Only the bytes are used:
    /// the browser's file name and content type are ignored. Returns null when there is no usable file.
    /// </summary>
    public static async Task<byte[]?> ReadAsync(HttpRequest request, StorageOptions options, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return null;
        }

        IFormCollection form = await request.ReadFormAsync(cancellationToken);
        IFormFile? file = form.Files.GetFile("file");
        if (file is null || file.Length == 0 || file.Length > options.MaxLogoBytes)
        {
            return null;
        }

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
