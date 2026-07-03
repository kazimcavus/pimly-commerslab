using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.ResolveAttributeChannelMapping;

/// <summary>ResolveAttributeChannelMappingQuery doğrulama kuralları.</summary>
public sealed class ResolveAttributeChannelMappingQueryValidator : AbstractValidator<ResolveAttributeChannelMappingQuery>
{
    public ResolveAttributeChannelMappingQueryValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.SourceType).NotEmpty();
        RuleFor(x => x.CatalogSourceId).NotEmpty();
    }
}
