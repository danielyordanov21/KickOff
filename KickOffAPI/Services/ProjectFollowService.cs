using Microsoft.EntityFrameworkCore;

public class ProjectFollowService(ProjectDbContext projectDbContext)
{
    private readonly ProjectDbContext _projectDbContext = projectDbContext;

    public async Task<ProjectFollowDto> FollowAsync(string projectId, string followerUserId)
    {
        var projectGuid = ParseGuid(projectId, "Invalid project id format.");
        var project = await GetRequiredProjectAsync(projectGuid);
        EnsureCanFollow(project, followerUserId);

        var existingFollow = await _projectDbContext.ProjectFollows
            .FirstOrDefaultAsync(follow =>
                follow.ProjectId == projectGuid &&
                follow.FollowerUserId == followerUserId);

        if (existingFollow == null)
        {
            await _projectDbContext.ProjectFollows.AddAsync(new ProjectFollow
            {
                ProjectId = projectGuid,
                FollowerUserId = followerUserId
            });

            await _projectDbContext.SaveChangesAsync();
        }

        return await GetStateAsync(projectGuid, followerUserId);
    }

    public async Task<ProjectFollowDto> UnfollowAsync(string projectId, string followerUserId)
    {
        var projectGuid = ParseGuid(projectId, "Invalid project id format.");
        var existingFollow = await _projectDbContext.ProjectFollows
            .FirstOrDefaultAsync(follow =>
                follow.ProjectId == projectGuid &&
                follow.FollowerUserId == followerUserId);

        if (existingFollow != null)
        {
            _projectDbContext.ProjectFollows.Remove(existingFollow);
            await _projectDbContext.SaveChangesAsync();
        }

        var followersCount = await _projectDbContext.ProjectFollows
            .AsNoTracking()
            .CountAsync(follow => follow.ProjectId == projectGuid);

        return new ProjectFollowDto
        {
            FollowersCount = followersCount,
            IsFollowing = false
        };
    }

    public async Task<ProjectFollowDto> UpdatePreferencesAsync(
        string projectId,
        string followerUserId,
        UpdateProjectFollowPreferencesDto dto)
    {
        var projectGuid = ParseGuid(projectId, "Invalid project id format.");
        var follow = await _projectDbContext.ProjectFollows
            .FirstOrDefaultAsync(existingFollow =>
                existingFollow.ProjectId == projectGuid &&
                existingFollow.FollowerUserId == followerUserId);

        if (follow == null)
            throw new KeyNotFoundException("Project follow not found.");

        follow.ReceiveInAppNotifications = dto.ReceiveInAppNotifications;
        follow.ReceiveEmailNotifications = dto.ReceiveEmailNotifications;

        await _projectDbContext.SaveChangesAsync();

        var followersCount = await _projectDbContext.ProjectFollows
            .AsNoTracking()
            .CountAsync(existingFollow => existingFollow.ProjectId == projectGuid);

        return new ProjectFollowDto
        {
            FollowersCount = followersCount,
            IsFollowing = true,
            ReceiveInAppNotifications = follow.ReceiveInAppNotifications,
            ReceiveEmailNotifications = follow.ReceiveEmailNotifications
        };
    }

    public async Task<ProjectFollowDto> GetStateAsync(Guid projectId, string? followerUserId)
    {
        var followersCount = await _projectDbContext.ProjectFollows
            .AsNoTracking()
            .CountAsync(follow => follow.ProjectId == projectId);

        if (string.IsNullOrWhiteSpace(followerUserId))
        {
            return new ProjectFollowDto
            {
                FollowersCount = followersCount,
                IsFollowing = false
            };
        }

        var existingFollow = await _projectDbContext.ProjectFollows
            .AsNoTracking()
            .FirstOrDefaultAsync(follow =>
                follow.ProjectId == projectId &&
                follow.FollowerUserId == followerUserId);

        return new ProjectFollowDto
        {
            FollowersCount = followersCount,
            IsFollowing = existingFollow != null,
            ReceiveInAppNotifications = existingFollow?.ReceiveInAppNotifications ?? true,
            ReceiveEmailNotifications = existingFollow?.ReceiveEmailNotifications ?? true
        };
    }

    private async Task<Project> GetRequiredProjectAsync(Guid projectId)
    {
        var project = await _projectDbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(existingProject => existingProject.Id == projectId);

        if (project == null)
            throw new KeyNotFoundException("Project not found.");

        return project;
    }

    private static void EnsureCanFollow(Project project, string followerUserId)
    {
        if (string.Equals(project.OwnerId, followerUserId, StringComparison.Ordinal))
            throw new InvalidOperationException("You can't follow your own project.");
    }

    private static Guid ParseGuid(string value, string errorMessage)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException(errorMessage);

        return guid;
    }
}
