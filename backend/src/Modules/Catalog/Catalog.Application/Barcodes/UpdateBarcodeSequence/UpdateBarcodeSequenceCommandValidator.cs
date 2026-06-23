using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Barcodes.UpdateBarcodeSequence;

/// <summary>UpdateBarcodeSequenceCommand için doğrulama kuralları.</summary>
public sealed class UpdateBarcodeSequenceCommandValidator : AbstractValidator<UpdateBarcodeSequenceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateBarcodeSequenceCommandValidator"/> class.
    /// </summary>
    public UpdateBarcodeSequenceCommandValidator()
    {
        RuleFor(x => x.NextValue)
            .GreaterThan(0)
            .WithMessage("Next value must be at least 1.");
    }
}
