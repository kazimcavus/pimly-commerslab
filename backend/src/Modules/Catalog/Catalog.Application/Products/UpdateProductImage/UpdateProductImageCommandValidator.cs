using Catalog.Application.Options;
using Catalog.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.Options;
using SharedKernel.Tenancy;

namespace Catalog.Application.Products.UpdateProductImage;

/// <summary>UpdateProductImageCommand için FluentValidation kuralları.</summary>
public sealed class UpdateProductImageCommandValidator : AbstractValidator<UpdateProductImageCommand>
{
    public UpdateProductImageCommandValidator(
        IOptions<MediaUrlOptions> mediaUrlOptions,
        ITenantContext tenantContext)
    {
        RuleFor(x => x.ImageId).RequiredId("ImageId");
        RuleFor(x => x.Url).ProductImageUrl(
            mediaUrlOptions.Value.AllowedUrlPrefix,
            tenantContext.TenantId);
        RuleFor(x => x.AltText).OptionalProductImageAltText();
    }
}
