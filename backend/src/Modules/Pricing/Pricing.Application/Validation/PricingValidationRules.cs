using FluentValidation;
using SharedKernel;

namespace Pricing.Application.Validation;

/// <summary>Pricing modülü için ortak doğrulama kuralları ve sabitleri.</summary>
internal static class PricingValidationRules
{
    public const int PriceDefinitionNameMaxLength = 200;
    public const int PriceDefinitionCodeMaxLength = 100;

    public static IRuleBuilderOptions<T, Guid> RequiredId<T>(
        this IRuleBuilder<T, Guid> ruleBuilder,
        string fieldName = "Id") =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.InvalidId)
            .WithMessage(ValidationMessages.InvalidId(fieldName));

    public static IRuleBuilderOptions<T, string> PriceDefinitionName<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Name"))
            .MaximumLength(PriceDefinitionNameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Name", PriceDefinitionNameMaxLength));

    public static IRuleBuilderOptions<T, string?> PriceDefinitionCode<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .MaximumLength(PriceDefinitionCodeMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Code", PriceDefinitionCodeMaxLength));
}
