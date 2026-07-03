using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.AttributeChannelMappings.ListAttributeValueChannelMappings;

/// <summary>ListAttributeValueChannelMappingsQuery doğrulama kuralları.</summary>
public sealed class ListAttributeValueChannelMappingsQueryValidator
    : AbstractValidator<ListAttributeValueChannelMappingsQuery>
{
    public ListAttributeValueChannelMappingsQueryValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.MappingId).NotEmpty();
    }
}
