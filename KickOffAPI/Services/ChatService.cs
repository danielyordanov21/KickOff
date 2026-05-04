using KickOffAPI.DTOs;
using Microsoft.AspNetCore.Identity;

namespace KickOffAPI.Services
{
    public class ChatService(
        SendbirdService sendbirdService,
        UserManager<ApplicationUser> userManager,
        UserService userService)
    {
        private readonly SendbirdService _sendbirdService = sendbirdService;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly UserService _userService = userService;

        public async Task<string> CreateSessionTokenAsync(string currentUserId, CancellationToken cancellationToken = default)
        {
            var user = await LoadRequiredCurrentUserAsync(currentUserId);
            await EnsureSendbirdUserAsync(user);
            return await _sendbirdService.CreateSessionToken(user.PublicId.ToString());
        }

        public async Task<string> CreateChannelAsync(
            string currentUserId,
            CreateChannelDto dto,
            CancellationToken cancellationToken = default)
        {
            var currentUser = await LoadRequiredCurrentUserAsync(currentUserId);

            if (!Guid.TryParse(dto.User2, out var recipientPublicId))
                throw new ArgumentException("A valid recipient is required to start a chat.");

            var recipient = await _userService.GetUser(recipientPublicId, cancellationToken);
            if (recipient == null)
                throw new KeyNotFoundException("The selected user could not be found.");

            if (recipient.PublicId == currentUser.PublicId)
                throw new InvalidOperationException("You cannot start a chat with yourself.");

            await EnsureSendbirdUserAsync(currentUser);
            await EnsureSendbirdUserAsync(recipient);

            return await _sendbirdService.CreateChannelAsync(
                currentUser.PublicId.ToString(),
                recipient.PublicId.ToString());
        }

        public async Task<string> GetChannelsAsync(string currentUserId, CancellationToken cancellationToken = default)
        {
            var user = await LoadRequiredCurrentUserAsync(currentUserId);
            return await _sendbirdService.GetUserChannels(user.PublicId.ToString());
        }

        private async Task<ApplicationUser> LoadRequiredCurrentUserAsync(string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            return user ?? throw new UnauthorizedAccessException("User not found.");
        }

        private Task EnsureSendbirdUserAsync(ApplicationUser user)
        {
            return _sendbirdService.EnsureUserAsync(
                user.PublicId.ToString(),
                ResolveChatNickname(user));
        }

        private static string ResolveChatNickname(ApplicationUser user)
        {
            return user.UserName?.Trim()
                ?? user.Email?.Trim()
                ?? user.PublicId.ToString();
        }
    }
}
