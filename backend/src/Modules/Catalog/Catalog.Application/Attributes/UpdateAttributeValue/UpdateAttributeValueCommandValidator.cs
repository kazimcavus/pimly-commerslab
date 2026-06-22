using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Attributes.UpdateAttributeValue;

/// <summary>UpdateAttributeValueCommand için doğrulama kurallarını tanımlar.</summary>
public sealed class UpdateAttributeValueCommandValidator : AbstractValidator<UpdateAttributeValueCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAttributeValueCommandValidator"/> class.
    /// </summary>
    public UpdateAttributeValueCommandValidator()
    {
        RuleFor(x => x.Id).RequiredId();
        RuleFor(x => x.Name).AttributeValueName();
    }
}
