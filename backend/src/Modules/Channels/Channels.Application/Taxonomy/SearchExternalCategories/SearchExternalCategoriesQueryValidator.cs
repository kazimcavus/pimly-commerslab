using Channels.Application.Validation;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.Taxonomy.SearchExternalCategories;

/// <summary>SearchExternalCategoriesQuery doğrulama kuralları.</summary>
public sealed class SearchExternalCategoriesQueryValidator : AbstractValidator<SearchExternalCategoriesQuery>
{
    public const int MaxLimit = 100;

    public SearchExternalCategoriesQueryValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, MaxLimit)
            .WithMessage($"Limit must be between 1 and {MaxLimit}.");
    }
}
