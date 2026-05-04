namespace KickOffAPI.Models;

/// <summary>
/// Generic paginated result containing data and pagination metadata
/// </summary>
public class PaginatedResult<T>
{
    public List<T> Data { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
