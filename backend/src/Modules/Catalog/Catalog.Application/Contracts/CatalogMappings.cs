using Catalog.Domain.Attributes;
using Catalog.Domain.Categories;
using Catalog.Domain.Variants;
using DomainAttribute = Catalog.Domain.Attributes.Attribute;
using DomainAttributeValue = Catalog.Domain.Attributes.AttributeValue;

namespace Catalog.Application.Contracts;

/// <summary>Domain modelleri ile DTO'lar arasında dönüşüm sağlayan eşleme yardımcı sınıfı.</summary>
internal static class CatalogMappings
{
    public static CategoryDto ToDto(this Category category) =>
        new(category.Id, category.Name, category.Code, category.ParentId);

    public static AttributeDto ToDto(this DomainAttribute attribute) =>
        new(attribute.Id, attribute.Key.Value, attribute.Name);

    public static AttributeDefinitionValueDto ToDto(this DomainAttributeValue value, Guid attributeId) =>
        new(value.Id, attributeId, value.Name);

    public static VariantTypeDto ToDto(this Variant variant) =>
        new(
            variant.Id,
            variant.Name,
            variant.SelectionStyle.ToString().ToLowerInvariant(),
            variant.SortOrder,
            variant.Slicer);

    public static VariantValueDto ToDto(this VariantValue value, Guid variantTypeId) =>
        new(
            value.Id,
            variantTypeId,
            value.Label,
            value.Color,
            value.ImageUrl,
            value.Code,
            value.SortOrder);

    public static SelectionStyle ParseSelectionStyle(string value) =>
        Enum.Parse<SelectionStyle>(value, true);
}
