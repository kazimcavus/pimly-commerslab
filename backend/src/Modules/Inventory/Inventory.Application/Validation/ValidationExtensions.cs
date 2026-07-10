using FluentValidation;
using SharedKernel;

namespace Inventory.Application.Validation;

/// <summary>FluentValidation sonuçlarını Result tipine dönüştüren uzantı metodları.</summary>
internal static class ValidationExtensions
{
    public static async Task<Result> ValidateToResultAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(instance, cancellationToken);
        if (validationResult.IsValid)
        {
            return Result.Success();
        }

        var errors = validationResult.Errors
            .Select(error =>
            {
                var code = string.IsNullOrWhiteSpace(error.ErrorCode)
                    ? ValidationErrorCodes.Unknown
                    : error.ErrorCode;
                return new ValidationError(error.PropertyName, code, error.ErrorMessage);
            })
            .ToList();

        return Result.Failure(Error.Validation("One or more validation errors occurred.", errors));
    }
}
