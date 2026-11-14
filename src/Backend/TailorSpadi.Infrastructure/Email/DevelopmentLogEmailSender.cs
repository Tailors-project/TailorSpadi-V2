using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TailorSpadi.Application.Abstractions;

namespace TailorSpadi.Infrastructure.Email;

/// <summary>Development only: optionally previews emails in the local console and debugger.</summary>
public sealed class DevelopmentLogEmailSender(
    ILogger<DevelopmentLogEmailSender> logger, IConfiguration configuration) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (configuration.GetValue<bool>("Email:ShowInConsole"))
        {
            string preview = $"--- LOCAL TEST EMAIL (not sent) ---{Environment.NewLine}"
                + $"To: {message.To}{Environment.NewLine}Subject: {message.Subject}{Environment.NewLine}{Environment.NewLine}"
                + $"{message.TextBody}{Environment.NewLine}--- END LOCAL TEST EMAIL ---";
            Console.WriteLine(preview);
            Debug.WriteLine(preview);
        }
        else
        {
            logger.LogInformation("Development email prepared with subject {Subject}; delivery is disabled and the body is omitted.", message.Subject);
        }
        return Task.CompletedTask;
    }
}
