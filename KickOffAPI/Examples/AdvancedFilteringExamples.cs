#if false
namespace KickOffAPI.Examples;

/// <summary>
/// Example usage patterns for Option C - Advanced filtering with pagination, sorting, and caching
/// 
/// This demonstrates how to use the new FilterSpecification pattern to build complex queries.
/// Copy these patterns into your ProjectService for production use.
/// </summary>
public static class AdvancedFilteringExamples
{
    /* ==================== EXAMPLE 1: Simple filtering ==================== */
    
    public static void Example1_SimpleStateFilter(ProjectRepository repository)
    {
        /*
        var spec = new ProjectFilterSpecification();
        spec.FilterByState(ProjectState.Active);
        spec.OptimizeForReadOnly();
        
        var projects = await repository.GetBySpecificationAsync(spec);
        */
    }

    /* ==================== EXAMPLE 2: Multiple criteria ==================== */
    
    public static void Example2_MultipleFilters(ProjectRepository repository)
    {
        /*
        var spec = new ProjectFilterSpecification("marketplace");  // search by goal
        spec.FilterByState(ProjectState.Active);
        spec.FilterByOwner("john@example.com");
        spec.SortByNewest();
        spec.OptimizeForReadOnly();
        
        var projects = await repository.GetBySpecificationAsync(spec);
        */
    }

    /* ==================== EXAMPLE 3: Pagination ==================== */
    
    public static void Example3_WithPagination(ProjectRepository repository)
    {
        /*
        var pageNumber = 1;
        var pageSize = 20;
        
        var spec = new ProjectFilterSpecification();
        spec.FilterByState(ProjectState.Active);
        spec.SortByNewest();
        spec.OptimizeForReadOnly();
        
        var result = await repository.GetPaginatedBySpecificationAsync(spec, pageNumber, pageSize);
        
        // Use result properties:
        // result.Data - the list of projects
        // result.TotalPages - total number of pages
        // result.HasNextPage - whether there's a next page
        // result.TotalCount - total number of items
        */
    }

    /* ==================== EXAMPLE 4: Custom sorting ==================== */
    
    public static void Example4_CustomSorting(ProjectRepository repository)
    {
        /*
        var spec = new ProjectFilterSpecification();
        spec.FilterByState(ProjectState.Active);
        spec.SortByOldest();  // oldest first
        spec.OptimizeForReadOnly();
        
        var projects = await repository.GetBySpecificationAsync(spec);
        */
    }

    /* ==================== EXAMPLE 5: With caching ==================== */
    
    public static void Example5_WithCaching(ProjectRepository repository, Services.CacheService cache)
    {
        /*
        var cacheKey = CacheService.GenerateKey("projects:active");
        
        // Check cache first
        var cached = await cache.GetAsync<List<Project>>(cacheKey);
        if (cached != null)
            return cached;
        
        // If not in cache, query database
        var spec = new ProjectFilterSpecification();
        spec.FilterByState(ProjectState.Active);
        spec.SortByNewest();
        spec.OptimizeForReadOnly();
        
        var projects = await repository.GetBySpecificationAsync(spec);
        
        // Cache for 60 minutes
        await cache.SetAsync(cacheKey, projects, TimeSpan.FromMinutes(60));
        
        return projects;
        */
    }

    /* ==================== EXAMPLE 6: Complex query builder ==================== */
    
    public static void Example6_ComplexQuery(ProjectRepository repository)
    {
        /*
        var spec = new ProjectFilterSpecification("api");  // goal contains "api"
        
        // Add multiple conditions
        spec.FilterByStates(ProjectState.Active, ProjectState.OnHold);
        spec.FilterByDateRange(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
        
        // Sort by newest, then by goal
        spec.SortByNewest();
        
        spec.SetPaging(pageNumber: 1, pageSize: 50);
        spec.OptimizeForReadOnly();
        
        var result = await repository.GetPaginatedBySpecificationAsync(spec, 1, 50);
        */
    }

    /* ==================== EXAMPLE 7: Building custom specifications ==================== */
    
    public static void Example7_CustomSpecification()
    {
        /*
        // Extend ProjectFilterSpecification for domain-specific logic
        
        public class ActiveRecentProjectsSpec : ProjectFilterSpecification
        {
            public ActiveRecentProjectsSpec(int daysBack = 7)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);
                
                AddFilter(p => p.State == ProjectState.Active);
                AddFilter(p => p.CreatedAt >= cutoffDate);
                AddOrderByDescending(p => p.CreatedAt);
                DisableTracking();
            }
        }
        
        // Usage:
        var spec = new ActiveRecentProjectsSpec(daysBack: 30);
        var recentActive = await repository.GetBySpecificationAsync(spec);
        */
    }
}
#endif
