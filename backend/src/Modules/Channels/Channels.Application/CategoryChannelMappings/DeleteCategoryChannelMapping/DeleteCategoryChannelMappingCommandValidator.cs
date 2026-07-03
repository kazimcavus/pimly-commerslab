using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.CategoryChannelMappings.DeleteCategoryChannelMapping;

/// <summary>DeleteCategoryChannelMappingCommand doğrulama kuralları.</summary>
public sealed class DeleteCategoryChannelMappingCommandValidator
    : AbstractValidator<DeleteCategoryChannelMappingCommand>
{
    public DeleteCategoryChannelMappingCommandValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
    }
}
