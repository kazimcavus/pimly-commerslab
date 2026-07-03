using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.GetCategoryChannelMapping;

/// <summary>GetCategoryChannelMappingQuery doğrulama kuralları.</summary>
public sealed class GetCategoryChannelMappingQueryValidator : AbstractValidator<GetCategoryChannelMappingQuery>
{
    public GetCategoryChannelMappingQueryValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
    }
}
