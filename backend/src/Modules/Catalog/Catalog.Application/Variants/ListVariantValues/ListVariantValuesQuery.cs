using SharedKernel;

namespace Catalog.Application.Variants.ListVariantValues;

/// <summary>Varyant değerlerini listeleme sorgusu.</summary>
public sealed record ListVariantValuesQuery(
    Guid VariantTypeId,
    int Page = Pagination.DefaultPage,
    int PageSize = Pagination.DefaultPageSize);
