using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace TailorSpadi.Application.Behaviors;

public sealed class RequestLoggingBehavior<TMessage, TResponse>(
    ILogger<RequestLoggingBehavior<TMessage, TResponse>> logger) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        long startedAt = Stopwatch.GetTimestamp();
        string requestName = typeof(TMessage).Name;

        try
        {
            TResponse response = await next(message, cancellationToken);
            logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds} ms",
                requestName, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Cancelled {RequestName}", requestName);
            throw;
        }
        catch (Exception exception)
        {
            // Log only the exception type: messages and request bodies may contain private data.
            logger.LogWarning("Failed {RequestName} with {ExceptionType}", requestName, exception.GetType().Name);
            throw;
        }
    }
}
