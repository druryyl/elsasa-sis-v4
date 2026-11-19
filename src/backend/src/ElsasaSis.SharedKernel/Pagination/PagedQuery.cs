namespace ElsasaSis.SharedKernel.Pagination;

public sealed record PagedQuery(
    int Page = 1,
    int PageSize = 20,
    string? SortField = null,
    string? SortOrder = null,
    string? Search = null)
{
    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int NormalizedPageSize => PageSize < 1 ? 20 : (PageSize > 100 ? 100 : PageSize);
    public int Offset => (NormalizedPage - 1) * NormalizedPageSize;
}
