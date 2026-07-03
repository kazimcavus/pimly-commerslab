using FluentValidation;
using SharedKernel;

namespace Identity.Application.Validation;

/// <summary>Identity modülü doğrulama kuralları.</summary>
internal static class IdentityValidationRules
{
    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email is not valid.");

    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage("Password is required.");

    public static IRuleBuilderOptions<T, string> RegisterPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.Password()
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
}
