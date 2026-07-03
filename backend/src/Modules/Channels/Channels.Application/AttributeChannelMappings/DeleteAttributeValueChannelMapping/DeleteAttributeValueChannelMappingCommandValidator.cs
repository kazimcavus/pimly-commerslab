using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.AttributeChannelMappings.DeleteAttributeValueChannelMapping;

/// <summary>DeleteAttributeValueChannelMappingCommand doğrulama kuralları.</summary>
public sealed class DeleteAttributeValueChannelMappingCommandValidator
    : AbstractValidator<DeleteAttributeValueChannelMappingCommand>
{
    public DeleteAttributeValueChannelMappingCommandValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.MappingId).NotEmpty();
        RuleFor(x => x.ValueMappingId).NotEmpty();
    }
}
