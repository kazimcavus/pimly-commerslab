using Catalog.Application.Options;
using Catalog.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.Options;
using SharedKernel.Tenancy;

namespace Catalog.Application.Variants.AddVariantValue;

/// <summary>AddVariantValueCommand için doğrulama kurallarını tanımlar.</summary>
public sealed class AddVariantValueCommandValidator : AbstractValidator<AddVariantValueCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddVariantValueCommandValidator"/> class.
    /// </summary>
    public AddVariantValueCommandValidator(
        IOptions<MediaUrlOptions> mediaUrlOptions,
        ITenantContext tenantContext)
    {
        RuleFor(x => x.VariantTypeId).RequiredId("VariantTypeId");
        RuleFor(x => x.Label).VariantValueLabel();
        RuleFor(x => x.Color).OptionalVariantValueColor();
        RuleFor(x => x.ImageUrl).OptionalVariantValueImageUrl(
            mediaUrlOptions.Value.AllowedUrlPrefix,
            tenantContext.TenantId);
        RuleFor(x => x.Key).OptionalVariantValueKey();
    }
}
