using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.DeleteAttributeValueChannelMapping;

/// <summary>DeleteAttributeValueChannelMappingCommand doğrulama kuralları.</summary>
public sealed class DeleteAttributeValueChannelMappingCommandValidator
    : AbstractValidator<DeleteAttributeValueChannelMappingCommand>
{
    public DeleteAttributeValueChannelMappingCommandValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.MappingId).NotEmpty();
        RuleFor(x => x.ValueMappingId).NotEmpty();
    }
}
