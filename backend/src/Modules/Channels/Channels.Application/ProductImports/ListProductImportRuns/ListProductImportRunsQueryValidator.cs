using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.ProductImports.ListProductImportRuns;

/// <summary>ListProductImportRunsQuery için doğrulama kuralları.</summary>
public sealed class ListProductImportRunsQueryValidator : AbstractValidator<ListProductImportRunsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListProductImportRunsQueryValidator"/> class.
    /// </summary>
    public ListProductImportRunsQueryValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100)
            .WithMessage("Limit must be between 1 and 100.");
    }
}
