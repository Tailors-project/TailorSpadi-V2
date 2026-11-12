using FluentValidation;
using Mediator;
using Microsoft.Extensions.Options;
using TailorSpadi.Application.Options;
using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.Application.Features.Onboarding;

/// <param name="Content">The raw uploaded bytes. The browser-supplied name and content type are never trusted or passed on.</param>
public sealed record UploadLogoCommand(byte[] Content) : ICommand<OnboardingState>;

public sealed class UploadLogoValidator : AbstractValidator<UploadLogoCommand>
{
    public UploadLogoValidator(IOptions<StorageOptions> options)
    {
        RuleFor(command => command.Content).NotNull().Must(content => content.Length > 0)
            .WithMessage("Choose an image file.")
            .Must(content => content.Length <= options.Value.MaxLogoBytes)
            .WithMessage("The image is too large.");
    }
}

public sealed class UploadLogoHandler(OnboardingService onboarding, LogoService logos)
    : ICommandHandler<UploadLogoCommand, OnboardingState>
{
    public async ValueTask<OnboardingState> Handle(UploadLogoCommand command, CancellationToken cancellationToken)
    {
        Guid userId = onboarding.RequireOnboardingUser();
        Tenant draft = await onboarding.RequireDraftAsync(userId, cancellationToken);
        await logos.ReplaceAsync(draft, command.Content, cancellationToken);
        return await onboarding.BuildStateAsync(draft, cancellationToken);
    }
}
