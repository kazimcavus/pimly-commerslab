using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.PriceDefinitions.CreatePriceDefinition;

/// <summary>CreatePriceDefinitionCommand için doğrulama kuralları.</summary>
public sealed class CreatePriceDefinitionCommandValidator : AbstractValidator<CreatePriceDefinitionCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePriceDefinitionCommandValidator"/> class.
    /// </summary>
    public CreatePriceDefinitionCommandValidator()
    {
        RuleFor(x => x.Name).PriceDefinitionName();
        RuleFor(x => x.Code).PriceDefinitionCode();
    }
}
