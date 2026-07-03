using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.AttributeChannelMappings.GetAttributeChannelMapping;

/// <summary>GetAttributeChannelMappingQuery doğrulama kuralları.</summary>
public sealed class GetAttributeChannelMappingQueryValidator : AbstractValidator<GetAttributeChannelMappingQuery>
{
    public GetAttributeChannelMappingQueryValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.MappingId).NotEmpty();
    }
}
