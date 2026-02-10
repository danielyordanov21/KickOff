using Microsoft.AspNetCore.Mvc;
using KickOffAPI.Models;
using KickOffAPI.Services;

/// <summary>
/// Example ProjectController using the new filtering specification pattern
/// This shows how to wire up the advanced filtering with proper API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProjectControllerExample(ProjectService ProjectService, CacheService CacheService) : ControllerBase
{
    private readonly ProjectService _ProjectService = ProjectService;
    private readonly CacheService _CacheService = CacheService;

    // ============ BASIC ENDPOINTS ============

    /// <summary>
    /// Get all projects (cached)
    /// </summary>
    [HttpGet("projects")]
    public async Task<IActionResult> GetAllProjects()
    {
        const string cacheKey = "projects:all";
        var cached = await _CacheService.GetAsync<List<Project>>(cacheKey);
        if (cached != null) return Ok(cached);

        var projects = await _ProjectService.GetCatalogueAsync();
        await _CacheService.SetAsync(cacheKey, projects, TimeSpan.FromMinutes(30));
        return Ok(projects);
    }

    // ============ FILTERING ENDPOINTS ============

    /// <summary>
    /// Get projects by state
    /// </summary>
    [HttpGet("projects/state/{state}")]
    public async Task<IActionResult> GetProjectsByState(string state)
    {
        if (!Enum.TryParse<ProjectState>(state, ignoreCase: true, out var projectState))
            return BadRequest($"Invalid state value. Valid values: {string.Join(", ", Enum.GetNames(typeof(ProjectState)))}");

        var cacheKey = CacheService.GenerateKey("projects:state", projectState);
        var cached = await _CacheService.GetAsync<List<Project>>(cacheKey);
        if (cached != null) return Ok(cached);

        var projects = await _ProjectService.GetProjectsByStateAsync(projectState);
        await _CacheService.SetAsync(cacheKey, projects, TimeSpan.FromMinutes(60));
        return Ok(projects);
    }

    // ============ ADVANCED FILTERING WITH PAGINATION ============

    /// <summary>
    /// Advanced project search with filtering, sorting, and pagination
    /// GET /api/project/search?pageNumber=1&pageSize=20&state=Active&keyword=marketplace&owner=john@example.com
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchProjects(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? state = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? owner = null,
        [FromQuery] bool sortNewest = true)
    {
        // Validate pagination parameters
        if (pageNumber < 1) return BadRequest("Page number must be >= 1");
        if (pageSize < 1 || pageSize > 100) return BadRequest("Page size must be between 1 and 100");

        // Parse state if provided
        ProjectState? projectState = null;
        if (!string.IsNullOrWhiteSpace(state))
        {
            if (!Enum.TryParse<ProjectState>(state, ignoreCase: true, out var parsed))
                return BadRequest($"Invalid state. Valid values: {string.Join(", ", Enum.GetNames(typeof(ProjectState)))}");
            projectState = parsed;
        }

        // Generate cache key for this specific query
        var cacheKey = CacheService.GenerateKey("projects:search", pageNumber, pageSize, state ?? "all", keyword ?? "any", owner ?? "any");
        var cached = await _CacheService.GetAsync<PaginatedResult<Project>>(cacheKey);
        if (cached != null) return Ok(cached);

        // Execute query
        var result = await _ProjectService.GetProjectsAsync(
            pageNumber, 
            pageSize, 
            projectState, 
            keyword, 
            owner, 
            sortNewest);

        // Cache for 15 minutes
        await _CacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));
        return Ok(result);
    }

    // ============ SEARCH ENDPOINTS ============

    /// <summary>
    /// Search projects by goal keyword
    /// GET /api/project/search-by-goal?keyword=marketplace
    /// </summary>
    [HttpGet("search-by-goal")]
    public async Task<IActionResult> SearchByGoal(
        [FromQuery] string keyword,
        [FromQuery] string? state = null)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest("Keyword is required");

        if (keyword.Length < 2)
            return BadRequest("Keyword must be at least 2 characters");

        // Parse state if provided
        ProjectState? projectState = null;
        if (!string.IsNullOrWhiteSpace(state))
        {
            if (!Enum.TryParse<ProjectState>(state, ignoreCase: true, out var parsed))
                return BadRequest($"Invalid state. Valid values: {string.Join(", ", Enum.GetNames(typeof(ProjectState)))}");
            projectState = parsed;
        }

        var cacheKey = CacheService.GenerateKey("projects:search-goal", keyword.ToLower(), state ?? "all");
        var cached = await _CacheService.GetAsync<List<Project>>(cacheKey);
        if (cached != null) return Ok(cached);

        var projects = await _ProjectService.SearchProjectsAsync(keyword, projectState);
        await _CacheService.SetAsync(cacheKey, projects, TimeSpan.FromMinutes(45));
        return Ok(projects);
    }

    // ============ FILTRATION + PAGINATION (NO KEYWORD) ============

    /// <summary>
    /// Get paginated projects by state (no keyword search)
    /// GET /api/project/paginated?state=Active&pageNumber=1&pageSize=20
    /// </summary>
    [HttpGet("paginated")]
    public async Task<IActionResult> GetPaginatedByState(
        [FromQuery] string? state = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) return BadRequest("Page number must be >= 1");
        if (pageSize < 1 || pageSize > 100) return BadRequest("Page size must be between 1 and 100");

        ProjectState? projectState = null;
        if (!string.IsNullOrWhiteSpace(state))
        {
            if (!Enum.TryParse<ProjectState>(state, ignoreCase: true, out var parsed))
                return BadRequest($"Invalid state. Valid values: {string.Join(", ", Enum.GetNames(typeof(ProjectState)))}");
            projectState = parsed;
        }

        var cacheKey = CacheService.GenerateKey("projects:paginated", state ?? "all", pageNumber, pageSize);
        var cached = await _CacheService.GetAsync<PaginatedResult<Project>>(cacheKey);
        if (cached != null) return Ok(cached);

        var result = await _ProjectService.GetProjectsAsync(pageNumber, pageSize, projectState);
        await _CacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
        return Ok(result);
    }

    // ============ EXAMPLE COMPLEX FILTERING ============

    /// <summary>
    /// Get projects filtered by owner with pagination
    /// GET /api/project/by-owner/john@example.com?pageNumber=1&pageSize=20
    /// </summary>
    [HttpGet("by-owner/{owner}")]
    public async Task<IActionResult> GetProjectsByOwner(
        string owner,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) return BadRequest("Page number must be >= 1");
        if (pageSize < 1 || pageSize > 100) return BadRequest("Page size must be between 1 and 100");

        var result = await _ProjectService.GetProjectsAsync(
            pageNumber: pageNumber,
            pageSize: pageSize,
            owner: owner,
            sortNewest: true);

        return Ok(result);
    }

    // ============ CACHE MANAGEMENT ============

    /// <summary>
    /// Clear project caches (admin only - add authorization in production)
    /// </summary>
    [HttpPost("cache/clear")]
    public async Task<IActionResult> ClearCache()
    {
        // In production, add [Authorize(Roles = "Admin")]
        await _CacheService.RemoveAsync("projects:all");
        await _CacheService.RemoveAsync(CacheService.GenerateKey("projects:state", ProjectState.Active));
        // Add more cache keys as needed
        return Ok("Cache cleared");
    }
}