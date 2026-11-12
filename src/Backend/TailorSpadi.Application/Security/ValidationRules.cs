using FluentValidation;

namespace TailorSpadi.Application.Security;

/// <summary>Reusable FluentValidation rules shared by the identity and onboarding commands.</summary>
public static class ValidationRules
{
    public static IRuleBuilderOptionsConditions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> rule)
    {
        return rule.Custom((value, context) =>
        {
            string? message = PasswordPolicy.Validate(value);
            if (message is not null)
            {
                context.AddFailure(message);
            }
        });
    }

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule)
    {
        return rule.NotEmpty().MaximumLength(254).EmailAddress().WithMessage("Enter a valid email address.");
    }

    public static IRuleBuilderOptions<T, string> ValidPersonName<T>(this IRuleBuilder<T, string> rule)
    {
        return rule.NotEmpty().MaximumLength(80);
    }

    public static IRuleBuilderOptions<T, string> ValidPhone<T>(this IRuleBuilder<T, string> rule)
    {
        return rule.NotEmpty().Matches(@"^[0-9+()\-\s]{7,20}$").WithMessage("Enter a valid phone number.");
    }

    public static IRuleBuilderOptions<T, string> ValidHexColor<T>(this IRuleBuilder<T, string> rule)
    {
        return rule.NotEmpty().Matches("^#[0-9a-fA-F]{6}$").WithMessage("Use a colour such as #1F4E79.");
    }
}
