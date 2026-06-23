using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Variants.UpdateVariantValue;

/// <summary>UpdateVariantValueCommand için doğrulama kurallarını tanımlar.</summary>
public sealed class UpdateVariantValueCommandValidator : AbstractValidator<UpdateVariantValueCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateVariantValueCommandValidator"/> class.
    /// </summary>
    public UpdateVariantValueCommandValidator()
    {
        RuleFor(x => x.Id).RequiredId();
        RuleFor(x => x.Label).VariantValueLabel();
        RuleFor(x => x.Color).OptionalVariantValueColor();
        RuleFor(x => x.ImageUrl).OptionalVariantValueImageUrl();
        RuleFor(x => x.Key).OptionalVariantValueKey();
    }
}
