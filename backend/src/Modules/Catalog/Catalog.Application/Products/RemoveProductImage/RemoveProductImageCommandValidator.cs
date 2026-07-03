using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Products.RemoveProductImage;

/// <summary>RemoveProductImageCommand için FluentValidation kuralları.</summary>
public sealed class RemoveProductImageCommandValidator : AbstractValidator<RemoveProductImageCommand>
{
    public RemoveProductImageCommandValidator()
    {
        RuleFor(x => x.ImageId).RequiredId("ImageId");
    }
}
