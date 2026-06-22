using SharedKernel;

namespace Catalog.Application.Categories.ListCategories;

/// <summary>Kategori listeleme sorgusu.</summary>
public sealed record ListCategoriesQuery(int Page = Pagination.DefaultPage, int PageSize = Pagination.DefaultPageSize);
