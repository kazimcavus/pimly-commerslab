using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Attributes.CreateAttribute;

/// <summary>CreateAttributeCommand için doğrulama kurallarını tanımlar.</summary>
public sealed class CreateAttributeCommandValidator : AbstractValidator<CreateAttributeCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAttributeCommandValidator"/> class.
    /// </summary>
    public CreateAttributeCommandValidator()
    {
        RuleFor(x => x.Name).AttributeName();
    }
}
