using SharedKernel;

namespace Catalog.Application.Attributes.ListAttributes;

/// <summary>Öznitelik listeleme sorgusu.</summary>
public sealed record ListAttributesQuery(int Page = Pagination.DefaultPage, int PageSize = Pagination.DefaultPageSize);
