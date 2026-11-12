using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Common;
using TailorSpadi.Application.Options;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Application.Features.Identity;

public sealed record ForgotPasswordCommand(string Email) : ICommand<Unit>;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(254);
    }
}

/// <summary>
/// Issues a single-use reset link by email. The outcome is identical whether or not the account exists,
/// so the endpoint cannot be used to discover registered emails.
/// </summary>
public sealed class ForgotPasswordHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    ITokenService tokens,
    IEmailSender email,
    IPublicUrlBuilder urls,
    IOptions<AuthOptions> authOptions,
    TimeProvider clock) : ICommandHandler<ForgotPasswordCommand, Unit>
{
    public async ValueTask<Unit> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        string normalized = User.NormalizeEmail(command.Email);
        User? user = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.NormalizedEmail == normalized && candidate.IsActive && !candidate.IsPlatformAdmin,
            cancellationToken);
        if (user is null)
        {
            return Unit.Value;
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        string raw = tokens.GenerateOpaqueToken();
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokens.HashToken(raw),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(authOptions.Value.ResetTokenMinutes)
        });
        db.Audit(clock, "password.reset-requested", user.Id, tenantContext.TenantId);
        await db.SaveChangesAsync(cancellationToken);

        string path = "/reset-password?token=" + Uri.EscapeDataString(raw);
        string link = tenantContext.Tenant is { } tenant ? urls.Tenant(tenant.Subdomain, path) : urls.Platform(path);
        await email.SendAsync(
            new EmailMessage(
                user.Email,
                "Reset your TailorSpadi password",
                $"Use this link to choose a new password. It works once and expires in {authOptions.Value.ResetTokenMinutes} minutes.\n\n{link}\n\nIf you did not ask for this, you can ignore this message."),
            cancellationToken);
        return Unit.Value;
    }
}
