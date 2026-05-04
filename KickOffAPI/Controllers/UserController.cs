using KickOffAPI.DTOs;
using KickOffAPI.Exceptions;
using KickOffAPI.Extensions;
using KickOffAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/users")]
public class UsersController(
    UserService userService,
    ILogger<UsersController> logger) : ControllerBase
{
    private readonly UserService _userService = userService;
    private readonly ILogger<UsersController> _logger = logger;

    [HttpGet("get-discover")]
    public async Task<IActionResult> GetDiscoverProfiles(CancellationToken cancellationToken)
    {
        var people = await _userService.GetDiscoverProducersAsync(cancellationToken);
        return Ok(people);
    }

    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> GetUserProfile(Guid publicId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.GetUserProfile(publicId, cancellationToken);

            if (user == null)
                return NotFound();

            return Ok(user);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("User profile request for {PublicId} was canceled by the client.", publicId);
            return new EmptyResult();
        }
    }

    [HttpGet("get-profile")]
    public Task<IActionResult> GetUserProfileDto([FromQuery] Guid id, CancellationToken cancellationToken)
    {
        return GetUserProfile(id, cancellationToken);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(
        UpdateUserProfileDto dto,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        try
        {
            var updatedProfile = await _userService.UpdateProfileAsync(currentUserId, dto, cancellationToken);
            return Ok(updatedProfile);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Profile update failed for user {UserId}.", currentUserId);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Profile update request for user {UserId} was canceled by the client.", currentUserId);
            return new EmptyResult();
        }
    }

    [HttpPost("profile-picture")]
    [Authorize]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        try
        {
            var profilePictureUrl = await _userService.UploadProfilePictureAsync(currentUserId, file, cancellationToken);
            return Ok(new { profilePictureUrl });
        }
        catch (IdentityOperationException ex)
        {
            _logger.LogError(
                "Failed to persist profile picture for user {UserId}. Errors: {Errors}",
                currentUserId,
                ex.Errors);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = ex.Message,
                errors = ex.Errors
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Profile picture upload request for user {UserId} was canceled by the client.", currentUserId);
            return new EmptyResult();
        }
    }

    [HttpPost("{publicId:guid}/follow")]
    [Authorize]
    public async Task<IActionResult> Follow(Guid publicId, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        try
        {
            await _userService.FollowUserAsync(currentUserId, publicId, cancellationToken);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{publicId:guid}/follow")]
    [Authorize]
    public async Task<IActionResult> Unfollow(Guid publicId, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        try
        {
            await _userService.UnfollowUserAsync(currentUserId, publicId, cancellationToken);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("become-producer")]
    [Authorize]
    public async Task<IActionResult> BecomeProducer(CancellationToken cancellationToken)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        try
        {
            var updatedProfile = await _userService.BecomeProducerAsync(currentUserId, cancellationToken);
            return Ok(updatedProfile);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Producer upgrade failed for user {UserId}.", currentUserId);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Producer upgrade request for user {UserId} was canceled by the client.", currentUserId);
            return new EmptyResult();
        }
    }

    [HttpPut("chat-preferences")]
    [Authorize]
    public async Task<IActionResult> UpdateChatPreferences(
        UpdateUserChatPreferencesDto dto,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetApplicationUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        try
        {
            var updatedProfile = await _userService.UpdateChatPreferencesAsync(currentUserId, dto, cancellationToken);
            return Ok(updatedProfile);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Chat preference update failed for user {UserId}.", currentUserId);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Chat preference update request for user {UserId} was canceled by the client.", currentUserId);
            return new EmptyResult();
        }
    }
}
