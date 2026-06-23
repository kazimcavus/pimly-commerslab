using Catalog.Application.Validation;
using FluentValidation;

using SharedKernel;

namespace Catalog.Application.SkuGenerator.UpdateSkuGeneratorConfig;

/// <summary>UpdateSkuGeneratorConfigCommand için doğrulama kuralları.</summary>
public sealed class UpdateSkuGeneratorConfigCommandValidator : AbstractValidator<UpdateSkuGeneratorConfigCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSkuGeneratorConfigCommandValidator"/> class.
    /// </summary>
    public UpdateSkuGeneratorConfigCommandValidator()
    {
        RuleFor(x => x.Segments).NotNull()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Segments"));

        RuleForEach(x => x.Segments).ChildRules(segment =>
        {
            segment.RuleFor(s => s.Type).NotEmpty()
                .WithErrorCode(ValidationErrorCodes.Required)
                .WithMessage(ValidationMessages.Required("Segment type"));
        });

        When(x => x.CounterNextValue.HasValue, () =>
        {
            RuleFor(x => x.CounterNextValue!.Value).GreaterThan(0)
                .WithMessage("Counter next value must be at least 1.");
        });
    }
}
