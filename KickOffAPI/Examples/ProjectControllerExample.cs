namespace KickOffAPI.Examples;

/// <summary>
/// Reference example showing how the advanced ProjectController integrates
/// filtering, caching, and pagination APIs.
/// This pattern is now implemented in Controllers/ProjectController.cs
/// </summary>
public static class ProjectControllerReferenceExample
{
    // The following endpoints are active in ProjectController:
    
    // [HttpGet("projects")]
    // Paginated list of all projects (default 20 per page)
    // GET /api/project/projects?pageNumber=1&pageSize=20
    
    // [HttpGet("projects-by-state/{state}")]
    // Get projects filtered by state
    // GET /api/project/projects-by-state/Active
    
    // [HttpGet("search")]
    // Advanced search with pagination, filtering by state/keyword/owner, and sorting
    // GET /api/project/search?pageNumber=1&pageSize=20&state=Active&keyword=marketplace&owner=user@example.com&sortNewest=true
    
    // [HttpGet("search-by-goal")]
    // Search by goal keyword with optional state filter
    // GET /api/project/search-by-goal?keyword=marketplace&state=Active
    
    // [HttpGet("paginated")]
    // Paginated projects with optional state filter
    // GET /api/project/paginated?state=Active&pageNumber=1&pageSize=20
    
    // [HttpGet("by-owner/{owner}")]
    // Get paginated projects filtered by owner
    // GET /api/project/by-owner/user@example.com?pageNumber=1&pageSize=20
    
    // Caching Strategy:
    // - Search results cached for 15 minutes
    // - Goal searches cached for 45 minutes
    // - Paginated results cached for 10 minutes
    // - State filters cached for 60 minutes
    
    // Implementation Details:
    // 1. All endpoints use the ProjectFilterSpecification pattern
    // 2. Results are returned as PaginatedResult<ProjectCatalogueDto> or List<ProjectCatalogueDto>
    // 3. Caching uses CacheService with key generation based on query parameters
    // 4. Invalid states and parameters return 400 BadRequest with helpful messages
    // 5. Read-only queries use AsNoTracking() for performance
}
