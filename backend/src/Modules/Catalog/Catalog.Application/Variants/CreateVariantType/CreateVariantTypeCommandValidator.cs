using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Variants.CreateVariantType;

/// <summary>CreateVariantTypeCommand için doğrulama kurallarını tanımlar.</summary>
public sealed class CreateVariantTypeCommandValidator : AbstractValidator<CreateVariantTypeCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateVariantTypeCommandValidator"/> class.
    /// </summary>
    public CreateVariantTypeCommandValidator()
    {
        RuleFor(x => x.Name).VariantTypeName();
        RuleFor(x => x.SelectionStyle).OptionalSelectionStyle();
    }
}
