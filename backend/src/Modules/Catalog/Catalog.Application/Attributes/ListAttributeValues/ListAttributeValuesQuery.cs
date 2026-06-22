using SharedKernel;

namespace Catalog.Application.Attributes.ListAttributeValues;

/// <summary>Özellik değerlerini listeleme sorgusu.</summary>
public sealed record ListAttributeValuesQuery(
    Guid AttributeId,
    int Page = Pagination.DefaultPage,
    int PageSize = Pagination.DefaultPageSize);
