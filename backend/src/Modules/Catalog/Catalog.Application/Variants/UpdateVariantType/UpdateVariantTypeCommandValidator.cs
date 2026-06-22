using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Variants.UpdateVariantType;

/// <summary>UpdateVariantTypeCommand için doğrulama kurallarını tanımlar.</summary>
public sealed class UpdateVariantTypeCommandValidator : AbstractValidator<UpdateVariantTypeCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateVariantTypeCommandValidator"/> class.
    /// </summary>
    public UpdateVariantTypeCommandValidator()
    {
        RuleFor(x => x.Id).RequiredId();
        RuleFor(x => x.Name).VariantTypeName();
        RuleFor(x => x.SelectionStyle).OptionalSelectionStyle();
    }
}
