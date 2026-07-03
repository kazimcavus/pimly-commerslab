using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.AttributeChannelMappings.UpsertAttributeChannelMapping;

/// <summary>UpsertAttributeChannelMappingCommand doğrulama kuralları.</summary>
public sealed class UpsertAttributeChannelMappingCommandValidator
    : AbstractValidator<UpsertAttributeChannelMappingCommand>
{
    public UpsertAttributeChannelMappingCommandValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.SourceType).NotEmpty();
        RuleFor(x => x.CatalogSourceId).NotEmpty();
        RuleFor(x => x.ExternalAttributeId).ExternalCategoryId();
    }
}
