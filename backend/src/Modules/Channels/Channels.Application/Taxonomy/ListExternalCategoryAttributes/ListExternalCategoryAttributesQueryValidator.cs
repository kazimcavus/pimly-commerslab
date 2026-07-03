using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.ListExternalCategoryAttributes;

public sealed class ListExternalCategoryAttributesQueryValidator : AbstractValidator<ListExternalCategoryAttributesQuery>
{
    public ListExternalCategoryAttributesQueryValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
    }
}
