using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.CategoryChannelMappings.ResolveCategoryChannelMapping;

/// <summary>ResolveCategoryChannelMappingQuery doğrulama kuralları.</summary>
public sealed class ResolveCategoryChannelMappingQueryValidator : AbstractValidator<ResolveCategoryChannelMappingQuery>
{
    public ResolveCategoryChannelMappingQueryValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
    }
}
