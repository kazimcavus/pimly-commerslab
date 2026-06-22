namespace SharedKernel;

/// <summary>Sayfalama parametreleri.</summary>
public sealed record Pagination(int Page, int PageSize)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Skip => (Page - 1) * PageSize;

    public static Result<Pagination> Create(int page, int pageSize)
    {
        if (page < DefaultPage)
        {
            return Result.Failure<Pagination>(Error.Validation("Page must be at least 1."));
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            return Result.Failure<Pagination>(
                Error.Validation($"Page size must be between 1 and {MaxPageSize}."));
        }

        return Result.Success(new Pagination(page, pageSize));
    }
}
