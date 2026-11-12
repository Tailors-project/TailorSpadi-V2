namespace TailorSpadi.Application.Abstractions;

public sealed record ProcessedImage(byte[] Content, string ContentType, string Extension);

public sealed class InvalidImageException(string message) : Exception(message);

public interface IImageProcessor
{
    /// <summary>
    /// Decodes the content, rejects anything that is not a PNG, JPEG or WebP image within the size limits,
    /// and returns a re-encoded copy without metadata. Throws <see cref="InvalidImageException"/> otherwise.
    /// </summary>
    ProcessedImage ProcessLogo(byte[] content);
}
