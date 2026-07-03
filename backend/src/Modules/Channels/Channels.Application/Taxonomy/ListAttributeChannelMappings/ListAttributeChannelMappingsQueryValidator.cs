using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.ListAttributeChannelMappings;

/// <summary>ListAttributeChannelMappingsQuery doğrulama kuralları.</summary>
public sealed class ListAttributeChannelMappingsQueryValidator : AbstractValidator<ListAttributeChannelMappingsQuery>
{
    public ListAttributeChannelMappingsQueryValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
    }
}
