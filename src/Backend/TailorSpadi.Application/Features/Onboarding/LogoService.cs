using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Features.Onboarding;

/// <summary>Validates, re-encodes, stores and swaps a tenant's logo without orphaning objects.</summary>
public sealed class LogoService(IImageProcessor images, IFileStorage storage, IAppDbContext db, TimeProvider clock)
{
    public async Task ReplaceAsync(Tenant tenant, byte[] content, CancellationToken cancellationToken)
    {
        ProcessedImage image;
        try
        {
            image = images.ProcessLogo(content);
        }
        catch (InvalidImageException exception)
        {
            throw ProblemException.BadRequest("invalid_image", exception.Message);
        }

        string newKey = $"logos/{tenant.Id:N}/{Guid.NewGuid():N}{image.Extension}";
        string? oldKey = tenant.LogoStorageKey;

        await storage.SaveAsync(newKey, image.Content, cancellationToken);
        tenant.LogoStorageKey = newKey;
        tenant.Touch(clock.GetUtcNow().UtcDateTime);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // The database change did not happen, so the new object must not stay behind.
            await storage.DeleteAsync(newKey, CancellationToken.None);
            throw;
        }

        if (oldKey is not null)
        {
            await storage.DeleteAsync(oldKey, CancellationToken.None);
        }
    }
}
