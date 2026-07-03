using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.GetTaxonomyStatus;

/// <summary>GetTaxonomyStatusQuery doğrulama kuralları.</summary>
public sealed class GetTaxonomyStatusQueryValidator : AbstractValidator<GetTaxonomyStatusQuery>
{
    public GetTaxonomyStatusQueryValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
    }
}
