using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.ListAttributeValueChannelMappings;

/// <summary>ListAttributeValueChannelMappingsQuery doğrulama kuralları.</summary>
public sealed class ListAttributeValueChannelMappingsQueryValidator
    : AbstractValidator<ListAttributeValueChannelMappingsQuery>
{
    public ListAttributeValueChannelMappingsQueryValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.MappingId).NotEmpty();
    }
}
