using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Products.ListItemPrices;

/// <summary>ListItemPricesQuery için doğrulama kuralları.</summary>
public sealed class ListItemPricesQueryValidator : AbstractValidator<ListItemPricesQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListItemPricesQueryValidator"/> class.
    /// </summary>
    public ListItemPricesQueryValidator()
    {
        RuleFor(x => x.ProductItemId).RequiredId();
    }
}
