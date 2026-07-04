using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Imports.GetProductImportRun;

/// <summary>GetProductImportRunQuery için doğrulama kuralları.</summary>
public sealed class GetProductImportRunQueryValidator : AbstractValidator<GetProductImportRunQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetProductImportRunQueryValidator"/> class.
    /// </summary>
    public GetProductImportRunQueryValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");
    }
}
