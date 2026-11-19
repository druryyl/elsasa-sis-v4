namespace ElsasaSis.SharedKernel.Pagination;

public sealed record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount <= 0 || PageSize <= 0
        ? 0
        : (int)Math.Ceiling((double)TotalCount / PageSize);
}
