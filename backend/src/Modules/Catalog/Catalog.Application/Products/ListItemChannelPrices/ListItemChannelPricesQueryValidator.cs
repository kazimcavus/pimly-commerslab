using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Products.ListItemChannelPrices;

/// <summary>ListItemChannelPricesQuery için doğrulama kuralları.</summary>
public sealed class ListItemChannelPricesQueryValidator : AbstractValidator<ListItemChannelPricesQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListItemChannelPricesQueryValidator"/> class.
    /// </summary>
    public ListItemChannelPricesQueryValidator()
    {
        RuleFor(x => x.ProductItemId).RequiredId();
    }
}
