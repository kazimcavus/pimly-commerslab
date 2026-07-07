using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Brands.UpdateBrand;

/// <summary>UpdateBrandCommand için doğrulama kuralları.</summary>
public sealed class UpdateBrandCommandValidator : AbstractValidator<UpdateBrandCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateBrandCommandValidator"/> class.
    /// </summary>
    public UpdateBrandCommandValidator()
    {
        RuleFor(x => x.Id).RequiredId();
        RuleFor(x => x.Name).BrandName();
        RuleFor(x => x.Code).BrandCode();
    }
}
