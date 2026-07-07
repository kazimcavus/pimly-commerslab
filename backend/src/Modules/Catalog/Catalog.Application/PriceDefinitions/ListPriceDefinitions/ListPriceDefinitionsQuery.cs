using SharedKernel;

namespace Catalog.Application.PriceDefinitions.ListPriceDefinitions;

/// <summary>Fiyat tanımı listeleme sorgusu.</summary>
public sealed record ListPriceDefinitionsQuery(int Page = Pagination.DefaultPage, int PageSize = Pagination.DefaultPageSize);
