namespace TailorSpadi.Application.Abstractions;

/// <summary>Stores binary objects under server-generated keys. Local disk now, cloud storage later.</summary>
public interface IFileStorage
{
    Task SaveAsync(string key, byte[] content, CancellationToken cancellationToken);
    Task<byte[]?> ReadAsync(string key, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
