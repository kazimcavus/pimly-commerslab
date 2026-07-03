using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Taxonomy.GetTaxonomySyncRun;

/// <summary>GetTaxonomySyncRunQuery doğrulama kuralları.</summary>
public sealed class GetTaxonomySyncRunQueryValidator : AbstractValidator<GetTaxonomySyncRunQuery>
{
    public GetTaxonomySyncRunQueryValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.SyncRunId).NotEmpty();
    }
}
