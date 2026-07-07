using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Products.DeleteItemPrice;

/// <summary>DeleteItemPriceCommand için doğrulama kuralları.</summary>
public sealed class DeleteItemPriceCommandValidator : AbstractValidator<DeleteItemPriceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteItemPriceCommandValidator"/> class.
    /// </summary>
    public DeleteItemPriceCommandValidator()
    {
        RuleFor(x => x.ProductItemId).RequiredId();
        RuleFor(x => x.PriceDefinitionId).RequiredId("PriceDefinitionId");
    }
}
