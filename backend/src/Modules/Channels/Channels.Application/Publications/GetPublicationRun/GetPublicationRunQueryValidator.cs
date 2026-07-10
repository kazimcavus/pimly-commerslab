using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Publications.GetPublicationRun;

/// <summary>GetPublicationRunQuery için doğrulama kuralları.</summary>
public sealed class GetPublicationRunQueryValidator : AbstractValidator<GetPublicationRunQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetPublicationRunQueryValidator"/> class.
    /// </summary>
    public GetPublicationRunQueryValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
        RuleFor(x => x.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");
    }
}
