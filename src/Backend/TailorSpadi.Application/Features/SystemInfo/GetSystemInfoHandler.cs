using Mediator;

namespace TailorSpadi.Application.Features.SystemInfo;

public sealed class GetSystemInfoHandler : IQueryHandler<GetSystemInfoQuery, SystemInfoResponse>
{
    public ValueTask<SystemInfoResponse> Handle(GetSystemInfoQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new SystemInfoResponse("TailorSpadi"));
    }
}
