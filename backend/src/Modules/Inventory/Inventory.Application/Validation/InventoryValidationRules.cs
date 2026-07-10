using FluentValidation;
using SharedKernel;

namespace Inventory.Application.Validation;

/// <summary>Inventory modülü için ortak doğrulama kuralları.</summary>
internal static class InventoryValidationRules
{
    public static IRuleBuilderOptions<T, Guid> RequiredId<T>(
        this IRuleBuilder<T, Guid> ruleBuilder,
        string fieldName = "Id") =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.InvalidId)
            .WithMessage(ValidationMessages.InvalidId(fieldName));
}
