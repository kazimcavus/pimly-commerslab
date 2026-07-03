using FluentValidation;
using SharedKernel;

namespace Channels.Application.Validation;

/// <summary>Channels modülü için ortak doğrulama kuralları.</summary>
internal static class ChannelsValidationRules
{
    public const int SellerIdMaxLength = 200;
    public const int ApiKeyMaxLength = 500;
    public const int ApiSecretMaxLength = 500;
    public const int ExternalCategoryIdMaxLength = 100;

    public static IRuleBuilderOptions<T, string> MarketplaceCode<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage("Marketplace code is required.");

    public static IRuleBuilderOptions<T, string> ApiKey<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage("Api key is required.")
            .MaximumLength(ApiKeyMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage($"Api key cannot exceed {ApiKeyMaxLength} characters.");

    public static IRuleBuilderOptions<T, string?> OptionalSellerId<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .MaximumLength(SellerIdMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage($"Seller id cannot exceed {SellerIdMaxLength} characters.");

    public static IRuleBuilderOptions<T, string?> OptionalApiSecret<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .MaximumLength(ApiSecretMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage($"Api secret cannot exceed {ApiSecretMaxLength} characters.");

    public static IRuleBuilderOptions<T, string> ExternalCategoryId<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage("External category id is required.")
            .MaximumLength(ExternalCategoryIdMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage($"External category id cannot exceed {ExternalCategoryIdMaxLength} characters.");
}
