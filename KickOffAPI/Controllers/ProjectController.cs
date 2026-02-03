using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProjectController(ProjectService ProjectService) : ControllerBase
{
private readonly ProjectService _ProjectService = ProjectService;

    [HttpGet("catalogue")]
    public async Task<IActionResult> GetCatalogue()
    {
        var Projects = await _ProjectService.GetCatalogueAsync()
            ?? throw new Exception("Failed to retrieve Project catalogue");
        return Ok(Projects);
    }
}