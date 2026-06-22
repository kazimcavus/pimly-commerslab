using SharedKernel;

namespace Catalog.Application;

/// <summary>Listeleme use case'leri için ortak sayfalama yardımcıları.</summary>
internal static class PaginationSupport
{
    internal static Result<Pagination> Resolve(int page, int pageSize) =>
        Pagination.Create(
            page <= 0 ? Pagination.DefaultPage : page,
            pageSize <= 0 ? Pagination.DefaultPageSize : pageSize);
}
