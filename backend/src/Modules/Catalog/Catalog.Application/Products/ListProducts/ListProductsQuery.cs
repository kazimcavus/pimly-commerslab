using SharedKernel;

namespace Catalog.Application.Products.ListProducts;

/// <summary>Ürün listeleme sorgusu.</summary>
public sealed record ListProductsQuery(int Page = Pagination.DefaultPage, int PageSize = Pagination.DefaultPageSize);
