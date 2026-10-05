using TailorSpadi.Application.Features.SystemInfo;

namespace TailorSpadi.UnitTests;

public sealed class SystemInfoTests
{
    [Fact]
    public async Task HandlerReturnsOnlyPublicMetadata()
    {
        var handler = new GetSystemInfoHandler();
        SystemInfoResponse result = await handler.Handle(new GetSystemInfoQuery(), CancellationToken.None);
        Assert.Equal("TailorSpadi", result.ApplicationName);
    }

    [Fact]
    public async Task HandlerHonorsCancellation()
    {
        var handler = new GetSystemInfoHandler();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await handler.Handle(new GetSystemInfoQuery(), cancellation.Token));
    }
}
