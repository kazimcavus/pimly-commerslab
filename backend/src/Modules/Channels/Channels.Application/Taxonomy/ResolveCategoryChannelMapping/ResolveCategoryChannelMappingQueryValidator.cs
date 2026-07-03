using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.ResolveCategoryChannelMapping;

/// <summary>ResolveCategoryChannelMappingQuery doğrulama kuralları.</summary>
public sealed class ResolveCategoryChannelMappingQueryValidator : AbstractValidator<ResolveCategoryChannelMappingQuery>
{
    public ResolveCategoryChannelMappingQueryValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
    }
}
