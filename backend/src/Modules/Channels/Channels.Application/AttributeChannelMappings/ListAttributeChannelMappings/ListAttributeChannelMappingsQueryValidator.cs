using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.AttributeChannelMappings.ListAttributeChannelMappings;

/// <summary>ListAttributeChannelMappingsQuery doğrulama kuralları.</summary>
public sealed class ListAttributeChannelMappingsQueryValidator : AbstractValidator<ListAttributeChannelMappingsQuery>
{
    public ListAttributeChannelMappingsQueryValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
    }
}
