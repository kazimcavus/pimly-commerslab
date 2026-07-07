using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Brands.CreateBrand;

/// <summary>CreateBrandCommand için doğrulama kuralları.</summary>
public sealed class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateBrandCommandValidator"/> class.
    /// </summary>
    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.Name).BrandName();
        RuleFor(x => x.Code).BrandCode();
    }
}
