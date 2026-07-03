using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.CategoryChannelMappings.GetCategoryChannelMapping;

/// <summary>GetCategoryChannelMappingQuery doğrulama kuralları.</summary>
public sealed class GetCategoryChannelMappingQueryValidator : AbstractValidator<GetCategoryChannelMappingQuery>
{
    public GetCategoryChannelMappingQueryValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
    }
}
