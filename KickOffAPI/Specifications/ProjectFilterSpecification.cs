namespace KickOffAPI.Specifications;

/// <summary>
/// Specification for Project filtering, sorting, and pagination
/// </summary>
public class ProjectFilterSpecification : FilterSpecification<Project>
{
    public ProjectFilterSpecification() { }

    /// <summary>
    /// Create specification with goal/name search
    /// </summary>
    public ProjectFilterSpecification(string? goalKeyword)
    {
        if (!string.IsNullOrWhiteSpace(goalKeyword))
        {
            AddFilter(p => p.Goal.Contains(goalKeyword));
        }
    }

    /// <summary>
    /// Filter by state (as enum)
    /// </summary>
    public void FilterByState(ProjectState state)
    {
        AddFilter(p => p.State == state);
    }

    /// <summary>
    /// Filter by owner
    /// </summary>
    public void FilterByOwner(string owner)
    {
        AddFilter(p => p.OwnerId == owner);
    }

    /// <summary>
    /// Filter by date range
    /// </summary>
    public void FilterByDateRange(DateTime startDate, DateTime endDate)
    {
        AddFilter(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate);
    }

    /// <summary>
    /// Filter by multiple states
    /// </summary>
    public void FilterByStates(params ProjectState[] states)
    {
        if (states.Length == 0) return;
        AddFilter(p => states.Contains(p.State));
    }

    /// <summary>
    /// Sort by goal ascending
    /// </summary>
    public void SortByGoalAscending()
    {
        AddOrderByAscending(p => p.Goal);
        AddOrderByAscending(p => p.Id);
    }

    /// <summary>
    /// Sort by goal descending
    /// </summary>
    public void SortByGoalDescending()
    {
        AddOrderByDescending(p => p.Goal);
        AddOrderByDescending(p => p.Id);
    }

    /// <summary>
    /// Sort by creation date descending (newest first)
    /// </summary>
    public void SortByNewest()
    {
        AddOrderByDescending(p => p.CreatedAt);
        AddOrderByDescending(p => p.Id);
    }

    /// <summary>
    /// Sort by creation date ascending (oldest first)
    /// </summary>
    public void SortByOldest()
    {
        AddOrderByAscending(p => p.CreatedAt);
        AddOrderByAscending(p => p.Id);
    }

    /// <summary>
    /// Apply pagination
    /// </summary>
    public void SetPaging(int pageNumber, int pageSize)
    {
        ApplyPaging(pageNumber, pageSize);
    }

    /// <summary>
    /// Optimize for read-only queries (disable tracking)
    /// </summary>
    public void OptimizeForReadOnly()
    {
        DisableTracking();
    }
}
