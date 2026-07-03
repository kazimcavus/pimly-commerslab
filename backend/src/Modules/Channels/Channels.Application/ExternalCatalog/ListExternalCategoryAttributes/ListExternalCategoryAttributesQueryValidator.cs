using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.ExternalCatalog.ListExternalCategoryAttributes;

public sealed class ListExternalCategoryAttributesQueryValidator : AbstractValidator<ListExternalCategoryAttributesQuery>
{
    public ListExternalCategoryAttributesQueryValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
    }
}
