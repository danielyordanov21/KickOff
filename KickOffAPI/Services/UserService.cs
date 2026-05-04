using KickOffAPI.DTOs;
using KickOffAPI.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KickOffAPI.Services
{
    public class UserService(
        UserManager<ApplicationUser> userManager,
        UserRepository userRepository,
        AppIdentityDbContext identityDbContext,
        ProjectDbContext projectDbContext,
        BlobService blobService,
        SendbirdService sendbirdService,
        IUserFollowService userFollowService,
        ILogger<UserService> logger)
    {
        private const string BackerRoleName = "Backer";
        private const string ProducerRoleName = "Producer";
        private const long MaxProfilePictureSizeBytes = 5 * 1024 * 1024;
        private static readonly string[] RoleDisplayPriority = ["Admin", ProducerRoleName, BackerRoleName, "User", "Guest"];
        private static readonly string[] SupportedChatLanguageCodes = ["de", "en", "es", "fr", "it", "ja", "ko", "pt", "ru", "zh"];
        private static readonly HashSet<string> SupportedChatLanguageCodeSet =
            [.. SupportedChatLanguageCodes];
        private sealed record AccountDeletionEligibility(bool CanDeleteAccount, string? RestrictionMessage);

        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly UserRepository _userRepository = userRepository;
        private readonly AppIdentityDbContext _identityDbContext = identityDbContext;
        private readonly ProjectDbContext _projectDbContext = projectDbContext;
        private readonly BlobService _blobService = blobService;
        private readonly SendbirdService _sendbirdService = sendbirdService;
        private readonly IUserFollowService _userFollowService = userFollowService;
        private readonly ILogger<UserService> _logger = logger;

        public async Task<ApplicationUser?> GetUser(Guid publicId, CancellationToken ct = default)
        {
            return await _userRepository.GetByPublicIdAsync(publicId, ct);
        }

        public async Task<UserProfileDto?> GetUserProfile(
            Guid publicId,
            CancellationToken ct = default,
            bool includeAccountManagement = false)
        {
            var user = await GetUser(publicId, ct);

            if (user == null)
            {
                return null;
            }

            var roles = OrderRoles(await _userManager.GetRolesAsync(user));
            var followers = await _userRepository.GetFollowersAsync(user.Id, ct);
            var following = await _userRepository.GetFollowingAsync(user.Id, ct);
            var projects = await GetOwnedProjectsAsync(user, ct);
            var backedProjects = await GetBackedProjectsAsync(user, ct);
            AccountDeletionEligibility? deletionEligibility = includeAccountManagement
                ? await BuildAccountDeletionEligibilityAsync(user.Id, ct)
                : null;

            return new UserProfileDto
            {
                Id = user.Id,
                IdP = user.PublicId.ToString(),
                Role = roles.FirstOrDefault(),
                Roles = [.. roles],
                State = user.State.ToString(),
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                UserName = user.UserName ?? string.Empty,
                ProfilePictureUrl = ResolveProfilePictureUrl(user.ProfilePictureUrl),
                PreferredChatLanguage = NormalizePreferredChatLanguage(user.PreferredChatLanguage),
                ShowOriginalChatTextByDefault = user.ShowOriginalChatTextByDefault,
                Projects = projects,
                BackedProjects = backedProjects,
                Followers = [.. followers.Select(MapProfileConnection)],
                Following = [.. following.Select(MapProfileConnection)],
                ProjectIds = [.. projects.Select(project => project.Id.ToString())],
                FollowerIdsP = [.. followers.Select(follower => follower.PublicId.ToString())],
                FollowingIdsP = [.. following.Select(followedUser => followedUser.PublicId.ToString())],
                CanDeleteAccount = deletionEligibility?.CanDeleteAccount,
                DeleteAccountRestriction = deletionEligibility?.RestrictionMessage
            };
        }

        public async Task<UserProfileDto> BecomeProducerAsync(string userId, CancellationToken ct = default)
        {
            var user = await GetRequiredUserByIdAsync(userId);

            var roles = await _userManager.GetRolesAsync(user);
            var isProducer = roles.Any(role =>
                role.Equals(ProducerRoleName, StringComparison.OrdinalIgnoreCase));

            if (!isProducer)
            {
                var addRoleResult = await _userManager.AddToRoleAsync(user, ProducerRoleName);
                if (!addRoleResult.Succeeded)
                {
                    throw new InvalidOperationException(FormatIdentityErrors(addRoleResult.Errors));
                }
            }

            return await GetUserProfile(user.PublicId, ct, includeAccountManagement: true)
                ?? throw new InvalidOperationException("Updated user profile could not be loaded.");
        }

        public async Task<UserProfileDto> UpdateProfileAsync(
            string userId,
            UpdateUserProfileDto dto,
            CancellationToken ct = default)
        {
            var user = await GetRequiredUserByIdAsync(userId);

            var normalizedUserName = dto.UserName?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedUserName))
            {
                throw new InvalidOperationException("Enter a username to continue.");
            }

            if (!string.Equals(user.UserName, normalizedUserName, StringComparison.Ordinal))
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, normalizedUserName);
                if (!setUserNameResult.Succeeded)
                {
                    throw new InvalidOperationException(FormatIdentityErrors(setUserNameResult.Errors));
                }

                await SyncSendbirdUserMetadataAsync(user, normalizedUserName, "profile update");
            }

            return await GetUserProfile(user.PublicId, ct, includeAccountManagement: true)
                ?? throw new InvalidOperationException("Updated user profile could not be loaded.");
        }

        public async Task<string> UploadProfilePictureAsync(
            string userId,
            IFormFile file,
            CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("File required");

            if (file.Length > MaxProfilePictureSizeBytes)
                throw new InvalidOperationException("Max file size is 5MB");

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only images allowed");

            var user = await GetRequiredUserByIdAsync(userId);
            var previousProfilePictureUrl = user.ProfilePictureUrl;
            string? uploadedBlobPath = null;

            try
            {
                uploadedBlobPath = await _blobService.UploadProfilePicture(user.Id, file);
                user.ProfilePictureUrl = uploadedBlobPath;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new IdentityOperationException(
                        "Profile picture could not be saved.",
                        updateResult.Errors);
                }

                if (!string.IsNullOrWhiteSpace(previousProfilePictureUrl) &&
                    !string.Equals(previousProfilePictureUrl, uploadedBlobPath, StringComparison.Ordinal))
                {
                    await DeleteProfilePictureIfPresentAsync(previousProfilePictureUrl);
                }

                await SyncSendbirdUserMetadataAsync(user, user.UserName, "profile picture update");

                return _blobService.GetReadUrl(uploadedBlobPath) ?? uploadedBlobPath;
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(uploadedBlobPath) &&
                    !string.Equals(previousProfilePictureUrl, uploadedBlobPath, StringComparison.Ordinal))
                {
                    await TryDeleteUploadedBlobAsync(uploadedBlobPath);
                }

                throw;
            }
        }

        public async Task FollowUserAsync(
            string followerUserId,
            Guid targetPublicId,
            CancellationToken ct = default)
        {
            var targetUser = await _userRepository.GetByPublicIdAsync(targetPublicId, ct);
            if (targetUser == null)
                throw new KeyNotFoundException("User not found.");

            await _userFollowService.FollowAsync(followerUserId, targetUser.Id);
        }

        public async Task UnfollowUserAsync(
            string followerUserId,
            Guid targetPublicId,
            CancellationToken ct = default)
        {
            var targetUser = await _userRepository.GetByPublicIdAsync(targetPublicId, ct);
            if (targetUser == null)
                throw new KeyNotFoundException("User not found.");

            await _userFollowService.UnfollowAsync(followerUserId, targetUser.Id);
        }

        public async Task<UserProfileDto> UpdateChatPreferencesAsync(
            string userId,
            UpdateUserChatPreferencesDto dto,
            CancellationToken ct = default)
        {
            var user = await GetRequiredUserByIdAsync(userId);

            var normalizedLanguage = NormalizePreferredChatLanguage(dto.PreferredChatLanguage);
            if (!string.IsNullOrWhiteSpace(dto.PreferredChatLanguage) && normalizedLanguage == null)
            {
                throw new InvalidOperationException(
                    $"Choose one of the supported chat languages: {string.Join(", ", SupportedChatLanguageCodes)}.");
            }

            user.PreferredChatLanguage = normalizedLanguage;
            user.ShowOriginalChatTextByDefault = dto.ShowOriginalChatTextByDefault;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(FormatIdentityErrors(updateResult.Errors));
            }

            return await GetUserProfile(user.PublicId, ct, includeAccountManagement: true)
                ?? throw new InvalidOperationException("Updated user profile could not be loaded.");
        }

        public async Task<(bool CanDeleteAccount, string? RestrictionMessage)> GetAccountDeletionEligibilityAsync(
            string userId,
            CancellationToken ct = default)
        {
            var eligibility = await BuildAccountDeletionEligibilityAsync(userId, ct);
            return (eligibility.CanDeleteAccount, eligibility.RestrictionMessage);
        }

        public async Task DeleteAccountAsync(string userId, CancellationToken ct = default)
        {
            var user = await GetRequiredUserByIdAsync(userId);

            var eligibility = await BuildAccountDeletionEligibilityAsync(userId, ct);
            if (!eligibility.CanDeleteAccount)
            {
                throw new InvalidOperationException(
                    eligibility.RestrictionMessage ?? "This account cannot be deleted right now.");
            }

            var userFollows = await _identityDbContext.UserFollows
                .Where(follow => follow.FollowerId == userId || follow.FollowingId == userId)
                .ToListAsync(ct);

            if (userFollows.Count > 0)
            {
                _identityDbContext.UserFollows.RemoveRange(userFollows);
                await _identityDbContext.SaveChangesAsync(ct);
            }

            var projectFollows = await _projectDbContext.ProjectFollows
                .Where(follow => follow.FollowerUserId == userId)
                .ToListAsync(ct);

            if (projectFollows.Count > 0)
            {
                _projectDbContext.ProjectFollows.RemoveRange(projectFollows);
            }

            var projectNotifications = await _projectDbContext.ProjectNotifications
                .Where(notification => notification.RecipientUserId == userId)
                .ToListAsync(ct);

            if (projectNotifications.Count > 0)
            {
                _projectDbContext.ProjectNotifications.RemoveRange(projectNotifications);
            }

            if (projectFollows.Count > 0 || projectNotifications.Count > 0)
            {
                await _projectDbContext.SaveChangesAsync(ct);
            }

            await DeleteProfilePictureIfPresentAsync(user.ProfilePictureUrl);

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                throw new InvalidOperationException(FormatIdentityErrors(deleteResult.Errors));
            }
        }

        public async Task<List<DiscoverPersonDto>> GetDiscoverProducersAsync(CancellationToken ct = default)
        {
            var producers = await _userRepository.GetRandomProducersAsync(20, ct);

            return producers
                .Select(UserToDiscoverDto)
                .Where(dto => dto != null)
                .Cast<DiscoverPersonDto>()
                .ToList();
        }

        public async Task<IdentityResult> ChangeUsername(string userId, string newName)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return IdentityResult.Failed();

            user.UserName = newName;

            return await _userManager.UpdateAsync(user);
        }

        private DiscoverPersonDto? UserToDiscoverDto(ApplicationUser user)
        {
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                return null;
            }

            return new DiscoverPersonDto
            {
                PublicId = user.PublicId,
                Username = user.UserName,
                ProfilePictureUrl = ResolveProfilePictureUrl(user.ProfilePictureUrl) ?? string.Empty
            };
        }

        private ProfileConnectionDto MapProfileConnection(ApplicationUser user)
        {
            return new ProfileConnectionDto
            {
                Id = user.Id,
                IdP = user.PublicId.ToString(),
                UserName = user.UserName ?? string.Empty,
                ProfilePictureUrl = ResolveProfilePictureUrl(user.ProfilePictureUrl),
                State = user.State.ToString()
            };
        }

        private async Task<List<ProjectCatalogueDto>> GetOwnedProjectsAsync(ApplicationUser user, CancellationToken ct)
        {
            var projects = await _projectDbContext.Projects
                .AsNoTracking()
                .Where(project => project.OwnerId == user.Id)
                .OrderByDescending(project => project.CreatedAt)
                .ThenByDescending(project => project.Id)
                .ToListAsync(ct);

            var ownerName = user.UserName ?? "Unknown";
            return [.. projects.Select(project => ToProfileProjectDto(project, ownerName))];
        }

        private async Task<List<ProjectCatalogueDto>> GetBackedProjectsAsync(ApplicationUser user, CancellationToken ct)
        {
            var allProjects = await _projectDbContext.Projects
                .AsNoTracking()
                .OrderByDescending(project => project.CreatedAt)
                .ThenByDescending(project => project.Id)
                .ToListAsync(ct);

            // Backer ids are stored as JSON, so we materialize the projects and filter in memory here.
            var backedProjects = allProjects
                .Where(project =>
                    !string.Equals(project.OwnerId, user.Id, StringComparison.Ordinal) &&
                    project.BackerIds.Any(backerId => string.Equals(backerId, user.Id, StringComparison.Ordinal)))
                .ToList();

            if (backedProjects.Count == 0)
            {
                return [];
            }

            var ownerIds = backedProjects
                .Select(project => project.OwnerId)
                .Distinct()
                .ToList();

            var ownerNames = await _identityDbContext.Users
                .AsNoTracking()
                .Where(owner => ownerIds.Contains(owner.Id))
                .ToDictionaryAsync(
                    owner => owner.Id,
                    owner => owner.UserName ?? "Unknown",
                    ct);

            return
            [
                .. backedProjects.Select(project =>
                {
                    var ownerName = ownerNames.TryGetValue(project.OwnerId, out var resolvedOwnerName)
                        ? resolvedOwnerName
                        : "Unknown";

                    return ToProfileProjectDto(project, ownerName);
                })
            ];
        }

        private ProjectCatalogueDto ToProfileProjectDto(Project project, string ownerName)
        {
            return new ProjectCatalogueDto
            {
                Id = project.Id,
                Name = project.Headline ?? project.Goal,
                Description = project.Description,
                Owner = ownerName,
                State = project.State.ToString(),
                FinancialGoal = project.FinancialGoal,
                FinancialRaised = project.FinancialRaised,
                EndDate = project.EndsAt,
                ImageUrl = ResolveProjectImageUrl(project.ImageUrls)
            };
        }

        private async Task<ApplicationUser> GetRequiredUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user ?? throw new UnauthorizedAccessException("User not found.");
        }

        private async Task SyncSendbirdUserMetadataAsync(
            ApplicationUser user,
            string? fallbackUserName,
            string operationName)
        {
            try
            {
                await _sendbirdService.UpdateUserAsync(
                    user.PublicId.ToString(),
                    user.UserName ?? fallbackUserName ?? user.Email ?? user.PublicId.ToString(),
                    user.ProfilePictureUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to sync Sendbird user metadata after {OperationName} for user {UserId}.",
                    operationName,
                    user.Id);
            }
        }

        private async Task TryDeleteUploadedBlobAsync(string blobName)
        {
            try
            {
                await _blobService.Delete(blobName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to clean up uploaded blob {BlobName} after a user profile update failure.",
                    blobName);
            }
        }

        private string? ResolveProfilePictureUrl(string? profilePictureUrl)
        {
            return _blobService.GetReadUrl(profilePictureUrl);
        }

        private string? ResolveProjectImageUrl(ICollection<string>? imageUrls)
        {
            var firstImageUrl = imageUrls?
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            return _blobService.GetReadUrl(firstImageUrl);
        }

        private static List<string> OrderRoles(IEnumerable<string> roles)
        {
            return roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetRolePriority)
                .ThenBy(role => role, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int GetRolePriority(string role)
        {
            var priority = Array.FindIndex(
                RoleDisplayPriority,
                candidate => candidate.Equals(role, StringComparison.OrdinalIgnoreCase));

            return priority >= 0 ? priority : int.MaxValue;
        }

        private static string? NormalizePreferredChatLanguage(string? preferredChatLanguage)
        {
            if (string.IsNullOrWhiteSpace(preferredChatLanguage))
            {
                return null;
            }

            var normalizedLanguage = preferredChatLanguage.Trim().ToLowerInvariant();
            return SupportedChatLanguageCodeSet.Contains(normalizedLanguage)
                ? normalizedLanguage
                : null;
        }

        private static string FormatIdentityErrors(IEnumerable<IdentityError> errors)
        {
            return string.Join(" ", errors
                .Select(error => error.Description)
                .Where(description => !string.IsNullOrWhiteSpace(description)));
        }

        private async Task<AccountDeletionEligibility> BuildAccountDeletionEligibilityAsync(
            string userId,
            CancellationToken ct)
        {
            var ownsProjects = await _projectDbContext.Projects
                .AsNoTracking()
                .AnyAsync(project => project.OwnerId == userId, ct);

            if (ownsProjects)
            {
                return new AccountDeletionEligibility(
                    false,
                    "Permanent deletion is disabled while this account still owns projects. Transfer or remove those projects first, or deactivate the account instead.");
            }

            var hasPublishedProjectUpdates = await _projectDbContext.ProjectUpdates
                .AsNoTracking()
                .AnyAsync(update => update.AuthorUserId == userId, ct);

            if (hasPublishedProjectUpdates)
            {
                return new AccountDeletionEligibility(
                    false,
                    "Permanent deletion is disabled because this account has published project updates. Deactivate the account instead.");
            }

            return new AccountDeletionEligibility(true, null);
        }

        private async Task DeleteProfilePictureIfPresentAsync(string? profilePictureUrl)
        {
            if (string.IsNullOrWhiteSpace(profilePictureUrl))
            {
                return;
            }

            try
            {
                await _blobService.Delete(profilePictureUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to delete profile picture blob {BlobName} while deleting an account.",
                    profilePictureUrl);
            }
        }
    }
}
