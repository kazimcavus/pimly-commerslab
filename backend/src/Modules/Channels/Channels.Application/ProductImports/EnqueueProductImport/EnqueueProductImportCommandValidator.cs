using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.ProductImports.EnqueueProductImport;

/// <summary>EnqueueProductImportCommand için doğrulama kuralları.</summary>
public sealed class EnqueueProductImportCommandValidator : AbstractValidator<EnqueueProductImportCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnqueueProductImportCommandValidator"/> class.
    /// </summary>
    public EnqueueProductImportCommandValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
    }
}
