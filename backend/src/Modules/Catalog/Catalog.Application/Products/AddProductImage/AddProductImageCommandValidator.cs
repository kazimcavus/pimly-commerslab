using Catalog.Application.Options;
using Catalog.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.Options;
using SharedKernel.Tenancy;

namespace Catalog.Application.Products.AddProductImage;

/// <summary>AddProductImageCommand için FluentValidation kuralları.</summary>
public sealed class AddProductImageCommandValidator : AbstractValidator<AddProductImageCommand>
{
    public AddProductImageCommandValidator(
        IOptions<MediaUrlOptions> mediaUrlOptions,
        ITenantContext tenantContext)
    {
        RuleFor(x => x.ProductId).RequiredId("ProductId");
        RuleFor(x => x.Url).ProductImageUrl(
            mediaUrlOptions.Value.AllowedUrlPrefix,
            tenantContext.TenantId);
        RuleFor(x => x.AltText).OptionalProductImageAltText();
    }
}
