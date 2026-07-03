using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.TaxonomySync.GetTaxonomyStatus;

/// <summary>GetTaxonomyStatusQuery doğrulama kuralları.</summary>
public sealed class GetTaxonomyStatusQueryValidator : AbstractValidator<GetTaxonomyStatusQuery>
{
    public GetTaxonomyStatusQueryValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
    }
}
