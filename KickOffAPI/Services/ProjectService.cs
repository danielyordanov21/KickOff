using KickOffAPI.Models;
using KickOffAPI.Specifications;

public class ProjectService(ProjectRepository ProjectRepository)
{
    private readonly ProjectRepository _ProjectRepository = ProjectRepository;

    /// <summary>
    /// Legacy method - kept for backward compatibility. Returns DTOs.
    /// </summary>
    public async Task<List<ProjectCatalogueDto>> GetCatalogueAsync(string? filter = null)
    {
        List<Project> projects;
        if (!string.IsNullOrEmpty(filter))
        {
            projects = await _ProjectRepository.FilterByStateAsync(filter);
        }
        else
        {
            projects = await _ProjectRepository.GetAllAsync();
        }

        return projects.Select(ToDto).ToList();
    }

    /// <summary>
    /// Get projects filtered by state using new specification pattern
    /// </summary>
    public async Task<List<ProjectCatalogueDto>> GetProjectsByStateAsync(ProjectState state)
    {
        var spec = new ProjectFilterSpecification();
        spec.FilterByState(state);
        spec.SortByNewest();
        spec.OptimizeForReadOnly();
        
        var projects = await _ProjectRepository.GetBySpecificationAsync(spec);
        return projects.Select(ToDto).ToList();
    }

    /// <summary>
    /// Get projects with pagination, filtering, and sorting
    /// </summary>
    public async Task<PaginatedResult<ProjectCatalogueDto>> GetProjectsAsync(
        int pageNumber = 1, 
        int pageSize = 10,
        ProjectState? state = null,
        string? goalKeyword = null,
        string? owner = null,
        bool sortNewest = true)
    {
        var spec = new ProjectFilterSpecification(goalKeyword);
        
        if (state.HasValue)
            spec.FilterByState(state.Value);
        
        if (!string.IsNullOrWhiteSpace(owner))
            spec.FilterByOwner(owner);
        
        spec.SetPaging(pageNumber, pageSize);
        
        if (sortNewest)
            spec.SortByNewest();
        
        spec.OptimizeForReadOnly();
        
        var paged = await _ProjectRepository.GetPaginatedBySpecificationAsync(spec, pageNumber, pageSize);

        return new PaginatedResult<ProjectCatalogueDto>
        {
            Data = paged.Data.Select(ToDto).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };
    }

    /// <summary>
    /// Search projects by goal with optional state filter
    /// </summary>
    public async Task<List<ProjectCatalogueDto>> SearchProjectsAsync(string goalKeyword, ProjectState? state = null)
    {
        var spec = new ProjectFilterSpecification(goalKeyword);
        
        if (state.HasValue)
            spec.FilterByState(state.Value);
        
        spec.SortByGoalAscending();
        spec.OptimizeForReadOnly();
        
        var projects = await _ProjectRepository.GetBySpecificationAsync(spec);
        return projects.Select(ToDto).ToList();
    }

    private static ProjectCatalogueDto ToDto(Project p)
    {
        return new ProjectCatalogueDto
        {
            Id = p.Id,
            Name = p.Goal,
            Description = p.Description,
            Owner = p.Owner,
            State = p.State.ToString()
        };
    }
}