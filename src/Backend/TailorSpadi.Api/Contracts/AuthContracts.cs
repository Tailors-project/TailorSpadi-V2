using TailorSpadi.Application.Features.Identity;
using TailorSpadi.Application.Features.Onboarding;

namespace TailorSpadi.Api.Contracts;

public sealed record RegisterRequest(
    string? FirstName, string? LastName, string? Email, string? Phone, string? Password, string? ConfirmPassword)
{
    public RegisterTailorCommand ToTailorCommand() => new(
        FirstName ?? string.Empty, LastName ?? string.Empty, Email ?? string.Empty,
        Phone ?? string.Empty, Password ?? string.Empty, ConfirmPassword ?? string.Empty);

    public RegisterCustomerCommand ToCustomerCommand() => new(
        FirstName ?? string.Empty, LastName ?? string.Empty, Email ?? string.Empty,
        Phone ?? string.Empty, Password ?? string.Empty, ConfirmPassword ?? string.Empty);
}

public sealed record LoginRequest(string? Email, string? Password);

public sealed record ForgotPasswordRequest(string? Email);

public sealed record ResetPasswordRequest(string? Token, string? NewPassword, string? ConfirmPassword);

public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword, string? ConfirmPassword);

public sealed record BusinessRequest(
    string? BusinessName,
    string? Description,
    string? Phone,
    string? WhatsApp,
    string? Email,
    string? Address,
    string? City,
    string? State,
    string? Country)
{
    public BusinessInfo ToBusinessInfo() => new(
        BusinessName ?? string.Empty,
        Description ?? string.Empty,
        Phone ?? string.Empty,
        WhatsApp ?? string.Empty,
        Email ?? string.Empty,
        Address ?? string.Empty,
        City ?? string.Empty,
        State ?? string.Empty,
        Country ?? string.Empty);
}

public sealed record BrandingRequest(string? PrimaryColor, string? SecondaryColor);

public sealed record SubdomainPreviewRequest(string? SlugBase);

public sealed record CompleteOnboardingRequest(Guid DraftId, string? SlugBase);
