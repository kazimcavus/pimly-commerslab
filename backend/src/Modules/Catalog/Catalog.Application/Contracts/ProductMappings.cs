using Catalog.Application.Products;
using Catalog.Domain.Products;
using ProductAttribute = Catalog.Domain.Products.Attribute;
using ProductAttributeValue = Catalog.Domain.Products.AttributeValue;
using ProductVariantValue = Catalog.Domain.Products.VariantValue;

namespace Catalog.Application.Contracts;

/// <summary>Product domain modelleri ile DTO'lar arasında dönüşüm sağlar.</summary>
internal static class ProductMappings
{
    public static ProductDto ToDto(this Product product, string? brandName = null) =>
        new(
            product.Id,
            product.GroupId,
            product.CategoryId,
            product.ModelCode.Value,
            product.Name,
            product.Status.ToString().ToLowerInvariant(),
            product.AttributeValues.Select(value => value.ToDto()).ToList(),
            product.Variants.Select(variant => variant.ToDto()).ToList(),
            product.Items.Select(item => item.ToDto(product.Id)).ToList(),
            product.Images.Select(image => image.ToDto()).ToList(),
            product.GroupCode,
            product.SlicerValue,
            product.BrandId,
            brandName,
            product.Description);

    public static ProductItemDto ToDto(this ProductItem item, Guid productId) =>
        new(
            item.Id,
            productId,
            item.Sku,
            item.Barcode,
            item.Gtin,
            item.Mpn,
            item.AxisValueEntryId,
            item.AxisValue,
            item.AttributeValues.Select(value => value.ToDto()).ToList(),
            item.VariantValues.Select(value => value.ToDto()).ToList());

    public static ProductStatus ParseStatus(string value) =>
        Enum.Parse<ProductStatus>(value, true);

    private static ProductAttributeValueDto ToDto(this ProductAttributeValue value) =>
        new(value.Attribute.ToDto(), value.Id, value.Name);

    private static AttributeDto ToDto(this ProductAttribute attribute) =>
        new(attribute.Id, attribute.Key, attribute.Name);

    private static ProductVariantDto ToDto(this Variant variant) =>
        new(
            variant.Id,
            variant.Name,
            variant.SelectionStyle.ToString().ToLowerInvariant(),
            variant.Slicer);

    private static ProductVariantValueDto ToDto(this ProductVariantValue value) =>
        new(value.Variant.ToDto(), value.Id, value.Name);

    internal static ProductImageDto ToDto(this ProductImage image) =>
        new(image.Id, image.Url, image.SortOrder, image.AltText, image.IsPrimary, image.VariantValueId);
}
