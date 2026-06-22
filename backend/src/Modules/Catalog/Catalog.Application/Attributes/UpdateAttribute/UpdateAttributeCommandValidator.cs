using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Attributes.UpdateAttribute;

/// <summary>UpdateAttributeCommand için doğrulama kurallarını tanımlar.</summary>
public sealed class UpdateAttributeCommandValidator : AbstractValidator<UpdateAttributeCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAttributeCommandValidator"/> class.
    /// </summary>
    public UpdateAttributeCommandValidator()
    {
        RuleFor(x => x.Id).RequiredId();
        RuleFor(x => x.Name).AttributeName();
    }
}
