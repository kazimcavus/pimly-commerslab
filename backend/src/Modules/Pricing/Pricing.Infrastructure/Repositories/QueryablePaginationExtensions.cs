using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Pricing.Infrastructure.Repositories;

/// <summary>EF Core sorguları için sayfalama yardımcıları.</summary>
internal static class QueryablePaginationExtensions
{
    internal static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        Pagination pagination,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, pagination.Page, pagination.PageSize, totalCount);
    }
}
