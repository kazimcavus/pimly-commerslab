namespace SharedKernel;

/// <summary>Sayfalanmış liste sonucu.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedResult<T> FromAll(IReadOnlyList<T> all, Pagination pagination) =>
        new(
            all.Skip(pagination.Skip).Take(pagination.PageSize).ToList(),
            pagination.Page,
            pagination.PageSize,
            all.Count);

    public PagedResult<TTarget> Map<TTarget>(Func<T, TTarget> selector) =>
        new(
            Items.Select(selector).ToList(),
            Page,
            PageSize,
            TotalCount);
}
