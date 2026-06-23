using FluentValidation;

namespace Catalog.Application.Barcodes.AllocateBarcodes;

/// <summary>AllocateBarcodesCommand için doğrulama kuralları.</summary>
public sealed class AllocateBarcodesCommandValidator : AbstractValidator<AllocateBarcodesCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AllocateBarcodesCommandValidator"/> class.
    /// </summary>
    public AllocateBarcodesCommandValidator()
    {
        RuleFor(x => x.Count)
            .GreaterThan(0)
            .WithMessage("Count must be at least 1.")
            .LessThanOrEqualTo(100)
            .WithMessage("Count cannot exceed 100.");
    }
}
