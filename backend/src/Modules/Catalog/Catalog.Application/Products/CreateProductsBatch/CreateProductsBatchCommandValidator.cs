using Catalog.Application.Products.CreateProduct;
using Catalog.Application.Validation;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.CreateProductsBatch;

/// <summary>CreateProductsBatchCommand için doğrulama kuralları.</summary>
public sealed class CreateProductsBatchCommandValidator : AbstractValidator<CreateProductsBatchCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProductsBatchCommandValidator"/> class.
    /// </summary>
    public CreateProductsBatchCommandValidator()
    {
        RuleFor(x => x.GroupId).RequiredGroupId();
        RuleFor(x => x.Products).NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Products"));
        RuleForEach(x => x.Products).SetValidator(new CreateProductsBatchItemValidator());
    }
}

/// <summary>CreateProductsBatchItem için doğrulama kuralları.</summary>
public sealed class CreateProductsBatchItemValidator : AbstractValidator<CreateProductsBatchItem>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProductsBatchItemValidator"/> class.
    /// </summary>
    public CreateProductsBatchItemValidator()
    {
        RuleFor(x => x.ModelCode).ModelCode();
        RuleFor(x => x.Name).ProductName();
        RuleFor(x => x.Status).ProductStatus();
        RuleFor(x => x.Items).NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Items"));
        RuleForEach(x => x.Items).SetValidator(new CreateProductItemInputValidator());
    }
}
