using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.AttributeChannelMappings.ResolveAttributeChannelMapping;

/// <summary>ResolveAttributeChannelMappingQuery doğrulama kuralları.</summary>
public sealed class ResolveAttributeChannelMappingQueryValidator : AbstractValidator<ResolveAttributeChannelMappingQuery>
{
    public ResolveAttributeChannelMappingQueryValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.SourceType).NotEmpty();
        RuleFor(x => x.CatalogSourceId).NotEmpty();
    }
}
