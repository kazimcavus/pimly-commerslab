using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.AttributeChannelMappings.UpsertAttributeValueChannelMappings;

/// <summary>UpsertAttributeValueChannelMappingsCommand doğrulama kuralları.</summary>
public sealed class UpsertAttributeValueChannelMappingsCommandValidator
    : AbstractValidator<UpsertAttributeValueChannelMappingsCommand>
{
    public UpsertAttributeValueChannelMappingsCommandValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.MappingId).NotEmpty();
        RuleFor(x => x.Values).NotNull();
        RuleForEach(x => x.Values).ChildRules(entry =>
        {
            entry.RuleFor(x => x.CatalogValueId).NotEmpty();
            entry.RuleFor(x => x.ExternalValueId).ExternalCategoryId();
        });
    }
}
