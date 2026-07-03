using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.DeleteCategoryChannelMapping;

/// <summary>DeleteCategoryChannelMappingCommand doğrulama kuralları.</summary>
public sealed class DeleteCategoryChannelMappingCommandValidator
    : AbstractValidator<DeleteCategoryChannelMappingCommand>
{
    public DeleteCategoryChannelMappingCommandValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
    }
}
