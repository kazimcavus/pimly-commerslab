using SharedKernel;

namespace Catalog.Application.Variants.ListVariantTypes;

/// <summary>Varyant türü listeleme sorgusu.</summary>
public sealed record ListVariantTypesQuery(int Page = Pagination.DefaultPage, int PageSize = Pagination.DefaultPageSize);
