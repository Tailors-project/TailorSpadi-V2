using Microsoft.Extensions.Logging;
using TailorSpadi.Application.Abstractions;

namespace TailorSpadi.Infrastructure.Email;

/// <summary>
/// Used outside Development until Module 7 adds real delivery. It drops the message and never logs its
/// content, because the body carries a one-time reset link.
/// </summary>
public sealed class UnconfiguredEmailSender(ILogger<UnconfiguredEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogWarning("Email delivery is not configured; a message was not sent.");
        return Task.CompletedTask;
    }
}
