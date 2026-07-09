using FluentValidation;
using Pricing.Application.Validation;

namespace Pricing.Application.ItemPrices.ListItemPrices;

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
