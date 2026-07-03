using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.TaxonomySync.GetTaxonomySyncRun;

/// <summary>GetTaxonomySyncRunQuery doğrulama kuralları.</summary>
public sealed class GetTaxonomySyncRunQueryValidator : AbstractValidator<GetTaxonomySyncRunQuery>
{
    public GetTaxonomySyncRunQueryValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.SyncRunId).NotEmpty();
    }
}
