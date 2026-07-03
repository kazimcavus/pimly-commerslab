using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.UpsertAttributeChannelMapping;

/// <summary>UpsertAttributeChannelMappingCommand doğrulama kuralları.</summary>
public sealed class UpsertAttributeChannelMappingCommandValidator
    : AbstractValidator<UpsertAttributeChannelMappingCommand>
{
    public UpsertAttributeChannelMappingCommandValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.SourceType).NotEmpty();
        RuleFor(x => x.CatalogSourceId).NotEmpty();
        RuleFor(x => x.ExternalAttributeId).ExternalCategoryId();
    }
}
