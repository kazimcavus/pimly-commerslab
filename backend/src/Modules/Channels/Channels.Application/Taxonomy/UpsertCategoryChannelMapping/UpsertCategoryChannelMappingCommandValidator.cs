using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.UpsertCategoryChannelMapping;

/// <summary>UpsertCategoryChannelMappingCommand doğrulama kuralları.</summary>
public sealed class UpsertCategoryChannelMappingCommandValidator
    : AbstractValidator<UpsertCategoryChannelMappingCommand>
{
    public UpsertCategoryChannelMappingCommandValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.ExternalId).ExternalCategoryId();
    }
}
