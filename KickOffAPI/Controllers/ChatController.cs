using KickOffAPI.DTOs;
using KickOffAPI.Extensions;
using KickOffAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KickOffAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController(ChatService chatService) : ControllerBase
    {
        private readonly ChatService _chatService = chatService;

        [HttpGet("token")]
        public async Task<IActionResult> GetToken(CancellationToken cancellationToken)
        {
            var currentUserId = User.GetApplicationUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            try
            {
                var token = await _chatService.CreateSessionTokenAsync(currentUserId, cancellationToken);
                return Ok(new { token });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        [HttpPost("channel")]
        public async Task<IActionResult> CreateChannel(CreateChannelDto dto, CancellationToken cancellationToken)
        {
            var currentUserId = User.GetApplicationUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            try
            {
                var channel = await _chatService.CreateChannelAsync(currentUserId, dto, cancellationToken);
                return Ok(new { channelUrl = channel });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("channels")]
        public async Task<IActionResult> GetChannels(CancellationToken cancellationToken)
        {
            var currentUserId = User.GetApplicationUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            try
            {
                var channels = await _chatService.GetChannelsAsync(currentUserId, cancellationToken);
                return Ok(channels);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }
    }
}
