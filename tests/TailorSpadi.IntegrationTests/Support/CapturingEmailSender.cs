using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TailorSpadi.Application.Abstractions;

namespace TailorSpadi.IntegrationTests.Support;

public sealed partial class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public IReadOnlyCollection<EmailMessage> Messages => _messages.ToArray();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>The reset token from the most recent message to the address, or null when none was sent.</summary>
    public string? LastResetTokenFor(string email)
    {
        EmailMessage? message = _messages.LastOrDefault(item => string.Equals(item.To, email, StringComparison.OrdinalIgnoreCase));
        Match? match = message is null ? null : TokenPattern().Match(message.TextBody);
        return match is { Success: true } ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
    }

    public string? LastLinkFor(string email)
    {
        EmailMessage? message = _messages.LastOrDefault(item => string.Equals(item.To, email, StringComparison.OrdinalIgnoreCase));
        Match? match = message is null ? null : LinkPattern().Match(message.TextBody);
        return match is { Success: true } ? match.Value : null;
    }

    [GeneratedRegex(@"token=([^\s]+)")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"https?://[^\s]+")]
    private static partial Regex LinkPattern();
}
