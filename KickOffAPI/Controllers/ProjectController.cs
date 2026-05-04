using System.Text.Json;
using KickOffAPI.Exceptions;
using KickOffAPI.Models;
using KickOffAPI.Extensions;
using KickOffAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProjectController(
    ProjectService projectService,
    ProjectFollowService projectFollowService,
    ProjectNotificationService projectNotificationService,
    ILogger<ProjectController> logger) : ControllerBase
{
    private readonly ProjectService _projectService = projectService;
    private readonly ProjectFollowService _projectFollowService = projectFollowService;
    private readonly ProjectNotificationService _projectNotificationService = projectNotificationService;
    private readonly ILogger<ProjectController> _logger = logger;

    [Authorize]
    [HttpPost]
    [RequestSizeLimit((6 * 8 * 1024 * 1024) + (2 * 1024 * 1024))]
    public async Task<IActionResult> CreateProject([FromForm] string project, [FromForm] List<IFormFile>? files)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            _logger.LogWarning("Project create rejected because no authenticated user id claim was available.");
            return Unauthorized();
        }

        try
        {
            var createdProject = await _projectService.CreateFromFormAsync(project, currentUserId, files);
            return CreatedAtAction(nameof(GetFullById), new { id = createdProject.Id }, createdProject);
        }
        catch (RequestValidationException ex)
        {
            _logger.LogWarning("Project create validation failed for user {UserId}.", currentUserId);
            return ValidationProblemResponse(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Project create forbidden for user {UserId}.", currentUserId);
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Project create received invalid JSON payload from user {UserId}.", currentUserId);
            return BadRequest("Invalid project payload.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Project create failed validation/business rules for user {UserId}.", currentUserId);
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    [RequestSizeLimit((6 * 8 * 1024 * 1024) + (2 * 1024 * 1024))]
    public async Task<IActionResult> UpdateProject(string id, [FromForm] string project, [FromForm] List<IFormFile>? files)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            _logger.LogWarning("Project update for {ProjectId} rejected because no authenticated user id claim was available.", id);
            return Unauthorized();
        }

        try
        {
            var updatedProject = await _projectService.UpdateFromFormAsync(id, project, currentUserId, files);
            return Ok(updatedProject);
        }
        catch (RequestValidationException ex)
        {
            _logger.LogWarning("Project update validation failed for project {ProjectId} by user {UserId}.", id, currentUserId);
            return ValidationProblemResponse(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Project update forbidden for project {ProjectId} by user {UserId}.", id, currentUserId);
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Project update failed because project {ProjectId} was not found for user {UserId}.", id, currentUserId);
            return NotFound(ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Project update received invalid JSON payload for project {ProjectId} from user {UserId}.", id, currentUserId);
            return BadRequest("Invalid project payload.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Project update failed validation/business rules for project {ProjectId} by user {UserId}.", id, currentUserId);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFullById(string id)
    {
        try
        {
            var dto = await _projectService.GetByIdAsync(id, User.GetApplicationUserId());
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("{id}/follow")]
    public async Task<IActionResult> FollowProject(string id)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        try
        {
            var follow = await _projectFollowService.FollowAsync(id, currentUserId);
            return Ok(follow);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Project follow failed because project {ProjectId} was not found.", id);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Project follow failed because project id {ProjectId} was invalid.", id);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Project follow rejected for project {ProjectId} by user {UserId}.", id, currentUserId);
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpDelete("{id}/follow")]
    public async Task<IActionResult> UnfollowProject(string id)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        try
        {
            var follow = await _projectFollowService.UnfollowAsync(id, currentUserId);
            return Ok(follow);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Project unfollow failed because project id {ProjectId} was invalid.", id);
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPut("{id}/follow/preferences")]
    public async Task<IActionResult> UpdateFollowPreferences(string id, [FromBody] UpdateProjectFollowPreferencesDto dto)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        try
        {
            var follow = await _projectFollowService.UpdatePreferencesAsync(id, currentUserId, dto);
            return Ok(follow);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Project follow preference update failed for project {ProjectId} by user {UserId}.", id, currentUserId);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Project follow preference update failed because project id {ProjectId} was invalid.", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/updates")]
    public async Task<IActionResult> GetUpdates(string id)
    {
        try
        {
            var updates = await _projectService.GetUpdatesAsync(id);
            return Ok(updates);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("{id}/updates")]
    public async Task<IActionResult> CreateUpdate(string id, [FromBody] SaveProjectUpdateDto dto)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            _logger.LogWarning("Project update create for {ProjectId} rejected because no authenticated user id claim was available.", id);
            return Unauthorized();
        }

        try
        {
            var createdUpdate = await _projectService.CreateUpdateAsync(id, dto, currentUserId);
            return Ok(createdUpdate);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Project update create forbidden for project {ProjectId} by user {UserId}.", id, currentUserId);
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Project update create failed because project {ProjectId} was not found for user {UserId}.", id, currentUserId);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Project update create failed validation for project {ProjectId} by user {UserId}.", id, currentUserId);
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPut("{id}/updates/{updateId}")]
    public async Task<IActionResult> UpdateProjectUpdate(string id, string updateId, [FromBody] SaveProjectUpdateDto dto)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            _logger.LogWarning("Project update edit for {ProjectId}/{UpdateId} rejected because no authenticated user id claim was available.", id, updateId);
            return Unauthorized();
        }

        try
        {
            var updatedUpdate = await _projectService.UpdateProjectUpdateAsync(id, updateId, dto, currentUserId);
            return Ok(updatedUpdate);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Project update edit forbidden for project {ProjectId}/{UpdateId} by user {UserId}.", id, updateId, currentUserId);
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Project update edit failed because project {ProjectId} or update {UpdateId} was not found for user {UserId}.", id, updateId, currentUserId);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Project update edit failed validation for project {ProjectId}/{UpdateId} by user {UserId}.", id, updateId, currentUserId);
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpDelete("{id}/updates/{updateId}")]
    public async Task<IActionResult> DeleteProjectUpdate(string id, string updateId)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            _logger.LogWarning("Project update delete for {ProjectId}/{UpdateId} rejected because no authenticated user id claim was available.", id, updateId);
            return Unauthorized();
        }

        try
        {
            await _projectService.DeleteProjectUpdateAsync(id, updateId, currentUserId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Project update delete forbidden for project {ProjectId}/{UpdateId} by user {UserId}.", id, updateId, currentUserId);
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Project update delete failed because project {ProjectId} or update {UpdateId} was not found for user {UserId}.", id, updateId, currentUserId);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Project update delete failed validation for project {ProjectId}/{UpdateId} by user {UserId}.", id, updateId, currentUserId);
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpGet("notifications")]
    public async Task<IActionResult> GetProjectNotifications([FromQuery] int take = 12, CancellationToken cancellationToken = default)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        var notifications = await _projectNotificationService.GetNotificationsAsync(
            currentUserId,
            take,
            cancellationToken);

        return Ok(notifications);
    }

    [Authorize]
    [HttpPost("notifications/{notificationId}/read")]
    public async Task<IActionResult> MarkProjectNotificationAsRead(string notificationId, CancellationToken cancellationToken = default)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        try
        {
            await _projectNotificationService.MarkAsReadAsync(notificationId, currentUserId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Project notification {NotificationId} was not found for user {UserId}.", notificationId, currentUserId);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Project notification id {NotificationId} was invalid for user {UserId}.", notificationId, currentUserId);
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> MarkAllProjectNotificationsAsRead(CancellationToken cancellationToken = default)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        await _projectNotificationService.MarkAllAsReadAsync(currentUserId, cancellationToken);
        return NoContent();
    }

    [HttpGet("projects")]
    public async Task<IActionResult> GetAllProjects(CancellationToken cancellationToken)
    {
        var projects = await _projectService.GetCachedCatalogueAsync(cancellationToken);
        return Ok(projects);
    }

    [HttpGet("projects/state/{state}")]
    public async Task<IActionResult> GetProjectsByState(string state, CancellationToken cancellationToken)
    {
        try
        {
            var projects = await _projectService.GetCachedProjectsByStateAsync(state, cancellationToken);
            return Ok(projects);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchProjects(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? state = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? owner = null,
        [FromQuery] bool sortNewest = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _projectService.SearchCachedProjectsAsync(
                pageNumber,
                pageSize,
                state,
                keyword,
                owner,
                sortNewest,
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("search-by-goal")]
    public async Task<IActionResult> SearchByGoal(
        [FromQuery] string keyword,
        [FromQuery] string? state = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var projects = await _projectService.SearchByGoalCachedAsync(keyword, state, cancellationToken);
            return Ok(projects);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("paginated")]
    public async Task<IActionResult> GetPaginated(
        [FromQuery] string? state = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _projectService.GetValidatedPaginatedAsync(state, pageNumber, pageSize);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("cache/clear")]
    public async Task<IActionResult> ClearCache(CancellationToken cancellationToken)
    {
        await _projectService.ClearCachedQueriesAsync(cancellationToken);
        return Ok("Cache cleared");
    }

    private BadRequestObjectResult ValidationProblemResponse(RequestValidationException exception)
    {
        return BadRequest(new ValidationProblemDetails(exception.Errors.ToDictionary(
            entry => entry.Key,
            entry => entry.Value))
        {
            Status = StatusCodes.Status400BadRequest
        });
    }
}
