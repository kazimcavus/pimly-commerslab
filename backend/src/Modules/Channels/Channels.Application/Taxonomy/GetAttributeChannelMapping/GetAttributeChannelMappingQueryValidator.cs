using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.GetAttributeChannelMapping;

/// <summary>GetAttributeChannelMappingQuery doğrulama kuralları.</summary>
public sealed class GetAttributeChannelMappingQueryValidator : AbstractValidator<GetAttributeChannelMappingQuery>
{
    public GetAttributeChannelMappingQueryValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.MappingId).NotEmpty();
    }
}
