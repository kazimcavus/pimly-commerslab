using Catalog.Application.Validation;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.CreateProduct;

/// <summary>CreateProductCommand için doğrulama kuralları.</summary>
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProductCommandValidator"/> class.
    /// </summary>
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.GroupId).RequiredGroupId();
        RuleFor(x => x.CategoryId).RequiredCategoryId();
        RuleFor(x => x.ModelCode)
            .MaximumLength(CatalogValidationRules.ModelCodeMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("ModelCode", CatalogValidationRules.ModelCodeMaxLength))
            .When(x => !string.IsNullOrWhiteSpace(x.ModelCode));
        RuleFor(x => x.Name).ProductName();
        RuleFor(x => x.Status).ProductStatus();
        RuleFor(x => x.Items).NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Items"));
        RuleForEach(x => x.Items).SetValidator(new CreateProductItemInputValidator());
    }
}

/// <summary>CreateProductItemInput için doğrulama kuralları.</summary>
public sealed class CreateProductItemInputValidator : AbstractValidator<CreateProductItemInput>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProductItemInputValidator"/> class.
    /// </summary>
    public CreateProductItemInputValidator()
    {
        RuleFor(x => x.Barcode).VariantBarcode();
        RuleFor(x => x.Sku).OptionalVariantSku();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0)
            .WithMessage("Price cannot be negative.");
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0)
            .WithMessage("Stock cannot be negative.");
    }
}
