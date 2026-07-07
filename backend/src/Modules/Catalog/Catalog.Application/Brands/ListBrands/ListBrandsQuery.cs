using SharedKernel;

namespace Catalog.Application.Brands.ListBrands;

/// <summary>Marka listeleme sorgusu.</summary>
public sealed record ListBrandsQuery(int Page = Pagination.DefaultPage, int PageSize = Pagination.DefaultPageSize);
