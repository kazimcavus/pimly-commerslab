using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Attributes.AddAttributeValue;

/// <summary>AddAttributeValueCommand için doğrulama kurallarını tanımlar.</summary>
public sealed class AddAttributeValueCommandValidator : AbstractValidator<AddAttributeValueCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddAttributeValueCommandValidator"/> class.
    /// </summary>
    public AddAttributeValueCommandValidator()
    {
        RuleFor(x => x.AttributeId).RequiredId("AttributeId");
        RuleFor(x => x.Name).AttributeValueName();
    }
}
