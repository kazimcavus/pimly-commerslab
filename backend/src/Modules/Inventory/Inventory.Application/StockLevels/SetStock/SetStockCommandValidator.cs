using FluentValidation;
using Inventory.Application.Validation;

namespace Inventory.Application.StockLevels.SetStock;

/// <summary>SetStockCommand için doğrulama kuralları.</summary>
public sealed class SetStockCommandValidator : AbstractValidator<SetStockCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetStockCommandValidator"/> class.
    /// </summary>
    public SetStockCommandValidator()
    {
        RuleFor(x => x.ProductItemId).RequiredId();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0)
            .WithMessage("Quantity cannot be negative.");
    }
}
