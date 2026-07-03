using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.DeleteAttributeChannelMapping;

/// <summary>DeleteAttributeChannelMappingCommand doğrulama kuralları.</summary>
public sealed class DeleteAttributeChannelMappingCommandValidator
    : AbstractValidator<DeleteAttributeChannelMappingCommand>
{
    public DeleteAttributeChannelMappingCommandValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.MappingId).NotEmpty();
    }
}
