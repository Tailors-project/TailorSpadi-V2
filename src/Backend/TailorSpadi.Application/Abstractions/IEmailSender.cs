namespace TailorSpadi.Application.Abstractions;

public sealed record EmailMessage(string To, string Subject, string TextBody);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
