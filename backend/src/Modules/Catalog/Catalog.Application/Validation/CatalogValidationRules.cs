using Catalog.Application.Contracts;
using Catalog.Domain.Barcodes;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Validation;

/// <summary>Katalog modülü için ortak doğrulama kuralları ve sabitleri.</summary>
internal static class CatalogValidationRules
{
    public const int CategoryNameMaxLength = 500;
    public const int CategoryCodeMaxLength = 100;
    public const int AttributeKeyMaxLength = 200;
    public const int AttributeNameMaxLength = 500;
    public const int AttributeValueNameMaxLength = 200;
    public const int VariantTypeNameMaxLength = 200;
    public const int VariantValueLabelMaxLength = 200;
    public const int VariantValueColorMaxLength = 50;
    public const int VariantValueImageUrlMaxLength = 2000;
    public const int ProductImageUrlMaxLength = 2000;
    public const int ProductImageAltTextMaxLength = 500;
    public const int VariantValueKeyMaxLength = 200;
    public const int ModelCodeMaxLength = 200;
    public const int ProductNameMaxLength = 500;
    public const int VariantBarcodeMaxLength = 200;
    public const int VariantSkuMaxLength = 200;

    public static IRuleBuilderOptions<T, Guid> RequiredId<T>(
        this IRuleBuilder<T, Guid> ruleBuilder,
        string fieldName = "Id") =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.InvalidId)
            .WithMessage(ValidationMessages.InvalidId(fieldName));

    public static IRuleBuilderOptions<T, string> CategoryName<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Name"))
            .MaximumLength(CategoryNameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Name", CategoryNameMaxLength));

    public static IRuleBuilderOptions<T, string?> CategoryCode<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .MaximumLength(CategoryCodeMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Code", CategoryCodeMaxLength));

    public static IRuleBuilderOptions<T, string> AttributeKey<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Key"))
            .MaximumLength(AttributeKeyMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Key", AttributeKeyMaxLength));

    public static IRuleBuilderOptions<T, string> AttributeName<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Name"))
            .MaximumLength(AttributeNameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Name", AttributeNameMaxLength));

    public static IRuleBuilderOptions<T, string> AttributeValueName<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Name"))
            .MaximumLength(AttributeValueNameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Name", AttributeValueNameMaxLength));

    public static IRuleBuilderOptions<T, string> VariantTypeName<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Name"))
            .MaximumLength(VariantTypeNameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Name", VariantTypeNameMaxLength));

    public static IRuleBuilderOptions<T, string> OptionalSelectionStyle<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .Must(BeValidOptionalSelectionStyle)
            .WithErrorCode(ValidationErrorCodes.InvalidEnum)
            .WithMessage(ValidationMessages.InvalidEnum("SelectionStyle"));

    public static IRuleBuilderOptions<T, string> VariantValueLabel<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Label"))
            .MaximumLength(VariantValueLabelMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Label", VariantValueLabelMaxLength));

    public static IRuleBuilderOptions<T, string?> OptionalVariantValueColor<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .MaximumLength(VariantValueColorMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Color", VariantValueColorMaxLength));

    public static IRuleBuilderOptions<T, string?> OptionalVariantValueImageUrl<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        string allowedUrlPrefix,
        Guid tenantId) =>
        ruleBuilder
            .MaximumLength(VariantValueImageUrlMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("ImageUrl", VariantValueImageUrlMaxLength))
            .Must(url => string.IsNullOrWhiteSpace(url) || IsAllowedMediaUrl(url, allowedUrlPrefix, tenantId))
            .WithErrorCode(ValidationErrorCodes.InvalidFormat)
            .WithMessage("ImageUrl must reference an uploaded media asset.");

    public static IRuleBuilderOptions<T, string> ProductImageUrl<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        string allowedUrlPrefix,
        Guid tenantId) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Url"))
            .MaximumLength(ProductImageUrlMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Url", ProductImageUrlMaxLength))
            .Must(url => IsAllowedMediaUrl(url, allowedUrlPrefix, tenantId))
            .WithErrorCode(ValidationErrorCodes.InvalidFormat)
            .WithMessage("Url must reference an uploaded media asset.");

    public static IRuleBuilderOptions<T, string?> OptionalProductImageAltText<T>(
        this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .MaximumLength(ProductImageAltTextMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("AltText", ProductImageAltTextMaxLength));

    public static IRuleBuilderOptions<T, string?> OptionalVariantTypeKey<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .MaximumLength(VariantValueKeyMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Key", VariantValueKeyMaxLength));

    public static IRuleBuilderOptions<T, string?> OptionalVariantValueKey<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .MaximumLength(VariantValueKeyMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Key", VariantValueKeyMaxLength));

    public static IRuleBuilderOptions<T, Guid> RequiredGroupId<T>(this IRuleBuilder<T, Guid> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.InvalidId)
            .WithMessage(ValidationMessages.InvalidId("GroupId"));

    public static IRuleBuilderOptions<T, Guid> RequiredCategoryId<T>(this IRuleBuilder<T, Guid> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.InvalidId)
            .WithMessage(ValidationMessages.InvalidId("CategoryId"));

    public static IRuleBuilderOptions<T, string> ModelCode<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("ModelCode"))
            .MaximumLength(ModelCodeMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("ModelCode", ModelCodeMaxLength));

    public static IRuleBuilderOptions<T, string> ProductName<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Name"))
            .MaximumLength(ProductNameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Name", ProductNameMaxLength));

    public static IRuleBuilderOptions<T, string> ProductStatus<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Status"))
            .Must(BeValidProductStatus)
            .WithErrorCode(ValidationErrorCodes.InvalidEnum)
            .WithMessage(ValidationMessages.InvalidEnum("Status"));

    public static IRuleBuilderOptions<T, string> VariantBarcode<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .WithMessage(ValidationMessages.Required("Barcode"))
            .MaximumLength(VariantBarcodeMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Barcode", VariantBarcodeMaxLength))
            .Must(BeNumericBarcode)
            .WithMessage("Barcode must be numeric.");

    public static IRuleBuilderOptions<T, string?> OptionalNumericBarcode<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .MaximumLength(VariantBarcodeMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Barcode", VariantBarcodeMaxLength))
            .Must(value => string.IsNullOrWhiteSpace(value) || BeNumericBarcode(value))
            .WithMessage("Barcode must be numeric.");

    public static IRuleBuilderOptions<T, string?> OptionalVariantSku<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .MaximumLength(VariantSkuMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage(ValidationMessages.MaxLength("Sku", VariantSkuMaxLength));

    private static bool BeNumericBarcode(string value) =>
        BarcodeAllocation.IsNumericBarcode(value.Trim());

    private static bool IsAllowedMediaUrl(string url, string allowedUrlPrefix, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmed = url.Trim();
        var tenantPrefix = $"{allowedUrlPrefix.TrimEnd('/')}/{tenantId:N}/";

        if (trimmed.StartsWith(tenantPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.AbsolutePath.StartsWith(
                tenantPrefix,
                StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool BeValidProductStatus(string value)
    {
        try
        {
            ProductMappings.ParseStatus(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool BeValidOptionalSelectionStyle(string value) =>
        string.IsNullOrWhiteSpace(value) || BeValidSelectionStyle(value);

    private static bool BeValidSelectionStyle(string value)
    {
        try
        {
            CatalogMappings.ParseSelectionStyle(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
