using SharedKernel;

namespace Catalog.Application.Categories.ListCategoryAttributes;

/// <summary>Kategori özelliklerini listeleme sorgusu.</summary>
public sealed record ListCategoryAttributesQuery(
    Guid CategoryId,
    int Page = Pagination.DefaultPage,
    int PageSize = Pagination.DefaultPageSize);
