using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.ListCategoryChannelMappings;

/// <summary>ListCategoryChannelMappingsQuery doğrulama kuralları.</summary>
public sealed class ListCategoryChannelMappingsQueryValidator : AbstractValidator<ListCategoryChannelMappingsQuery>
{
    public ListCategoryChannelMappingsQueryValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
    }
}
