using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.CategoryChannelMappings.UpsertCategoryChannelMapping;

/// <summary>UpsertCategoryChannelMappingCommand doğrulama kuralları.</summary>
public sealed class UpsertCategoryChannelMappingCommandValidator
    : AbstractValidator<UpsertCategoryChannelMappingCommand>
{
    public UpsertCategoryChannelMappingCommandValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.CatalogCategoryId).NotEmpty();
        RuleFor(x => x.ExternalId).ExternalCategoryId();
    }
}
