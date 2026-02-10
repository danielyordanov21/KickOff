using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProjectController(ProjectService ProjectService) : ControllerBase
{
    private readonly ProjectService _ProjectService = ProjectService;

    [HttpGet("projects")]
    public async Task<IActionResult> GetCatalogue([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) return BadRequest("Page number must be >= 1");
        if (pageSize < 1 || pageSize > 500) return BadRequest("Page size must be between 1 and 500");

        var result = await _ProjectService.GetProjectsAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("projects-by-state/{state}")]
    public async Task<IActionResult> GetCatalogueByState(string state)
    {
        if (!Enum.TryParse<ProjectState>(state, ignoreCase: true, out var parsed))
            return BadRequest($"Invalid state value. Valid values: {string.Join(", ", Enum.GetNames(typeof(ProjectState)))}");

        var projects = await _ProjectService.GetProjectsByStateAsync(parsed);
        return Ok(projects);
    }
}