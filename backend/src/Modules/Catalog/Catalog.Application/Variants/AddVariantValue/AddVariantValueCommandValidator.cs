using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Variants.AddVariantValue;

/// <summary>AddVariantValueCommand için doğrulama kurallarını tanımlar.</summary>
public sealed class AddVariantValueCommandValidator : AbstractValidator<AddVariantValueCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddVariantValueCommandValidator"/> class.
    /// </summary>
    public AddVariantValueCommandValidator()
    {
        RuleFor(x => x.VariantTypeId).RequiredId("VariantTypeId");
        RuleFor(x => x.Label).VariantValueLabel();
        RuleFor(x => x.Color).OptionalVariantValueColor();
        RuleFor(x => x.ImageUrl).OptionalVariantValueImageUrl();
        RuleFor(x => x.Code).OptionalVariantValueCode();
    }
}
