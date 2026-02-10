using System.Linq.Expressions;

namespace KickOffAPI.Specifications;

/// <summary>
/// Base specification class for building complex queries with filtering, sorting, and pagination.
/// </summary>
public abstract class FilterSpecification<T> where T : class
{
    /// <summary>
    /// List of filter predicates to apply
    /// </summary>
    public List<Expression<Func<T, bool>>> Filters { get; } = [];

    /// <summary>
    /// List of properties to include (Include navigation properties)
    /// </summary>
    public List<Expression<Func<T, object>>> Includes { get; } = [];

    /// <summary>
    /// Sorting order: 0 = ascending, 1 = descending
    /// </summary>
    public List<(Expression<Func<T, object>> KeySelector, int SortOrder)> OrderBy { get; } = [];

    /// <summary>
    /// Pagination: number of items to skip
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// Pagination: number of items to take
    /// </summary>
    public int? Take { get; set; }

    /// <summary>
    /// Whether to track entities or not (for performance optimization)
    /// </summary>
    public bool IsPagingEnabled { get; set; } = false;

    public bool IsTrackingEnabled { get; set; } = true;

    /// <summary>
    /// Add a filter predicate
    /// </summary>
    protected virtual void AddFilter(Expression<Func<T, bool>> filter)
    {
        Filters.Add(filter);
    }

    /// <summary>
    /// Add a property to include
    /// </summary>
    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Add ascending sort
    /// </summary>
    protected virtual void AddOrderByAscending(Expression<Func<T, object>> keySelector)
    {
        OrderBy.Add((keySelector, 0));
    }

    /// <summary>
    /// Add descending sort
    /// </summary>
    protected virtual void AddOrderByDescending(Expression<Func<T, object>> keySelector)
    {
        OrderBy.Add((keySelector, 1));
    }

    /// <summary>
    /// Enable pagination
    /// </summary>
    public virtual void ApplyPaging(int pageNumber, int pageSize)
    {
        IsPagingEnabled = true;
        Skip = (pageNumber - 1) * pageSize;
        Take = pageSize;
    }

    /// <summary>
    /// Disable entity tracking (useful for read-only queries)
    /// </summary>
    protected virtual void DisableTracking()
    {
        IsTrackingEnabled = false;
    }
}
