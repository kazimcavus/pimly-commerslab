using Catalog.Application.Contracts;
using Catalog.Domain.Categories;

namespace Catalog.Application.Categories;

/// <summary>Kategori-özellik atamasını DTO'ya dönüştüren eşleme yardımcı sınıfı.</summary>
internal static class CategoryAttributeMapping
{
    internal static CategoryAttributeDto ToDto(
        CategoryAttributeAssignment assignment,
        Domain.Attributes.Attribute attribute) =>
        new(
            assignment.Id,
            attribute.Id,
            attribute.Key.Value,
            attribute.Name,
            assignment.Required,
            assignment.SortOrder,
            assignment.Scope.ToString().ToLowerInvariant());
}
