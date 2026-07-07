using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.PriceDefinitions.UpdatePriceDefinition;

/// <summary>UpdatePriceDefinitionCommand için doğrulama kuralları.</summary>
public sealed class UpdatePriceDefinitionCommandValidator : AbstractValidator<UpdatePriceDefinitionCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatePriceDefinitionCommandValidator"/> class.
    /// </summary>
    public UpdatePriceDefinitionCommandValidator()
    {
        RuleFor(x => x.Id).RequiredId();
        RuleFor(x => x.Name).PriceDefinitionName();
        RuleFor(x => x.Code).PriceDefinitionCode();
    }
}
