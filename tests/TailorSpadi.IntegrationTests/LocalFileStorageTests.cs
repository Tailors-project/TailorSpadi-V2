using Microsoft.Extensions.Options;
using TailorSpadi.Application.Options;
using TailorSpadi.Infrastructure.Storage;

namespace TailorSpadi.IntegrationTests;

public sealed class LocalFileStorageTests
{
    [Fact]
    public async Task CancelledWriteLeavesNoTemporaryOrFinalFile()
    {
        string root = Path.Combine(Path.GetTempPath(), "tailorspadi-storage-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(Options.Create(new StorageOptions { Root = root }));
        try
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.SaveAsync("logos/test.png", [1, 2, 3], cancellation.Token));
            Assert.Empty(Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
