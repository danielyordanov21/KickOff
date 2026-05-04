using KickOffAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KickOffAPI.Tests.Services;

public class ProjectFollowServiceTests
{
    [Fact]
    public async Task FollowAsync_CreatesASingleFollowRecord_AndReturnsCurrentState()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var projectContext = database.CreateProjectContext();

        var project = TestDataFactory.CreateProject("owner-1");
        projectContext.Projects.Add(project);
        await projectContext.SaveChangesAsync();

        var service = new ProjectFollowService(projectContext);

        var firstResult = await service.FollowAsync(project.Id.ToString(), "follower-1");
        var secondResult = await service.FollowAsync(project.Id.ToString(), "follower-1");

        Assert.True(firstResult.IsFollowing);
        Assert.Equal(1, firstResult.FollowersCount);
        Assert.True(secondResult.IsFollowing);
        Assert.Equal(1, secondResult.FollowersCount);
        Assert.Equal(1, await projectContext.ProjectFollows.CountAsync());
    }

    [Fact]
    public async Task FollowAsync_RejectsFollowingYourOwnProject()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var projectContext = database.CreateProjectContext();

        var project = TestDataFactory.CreateProject("owner-1");
        projectContext.Projects.Add(project);
        await projectContext.SaveChangesAsync();

        var service = new ProjectFollowService(projectContext);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FollowAsync(project.Id.ToString(), "owner-1"));

        Assert.Equal("You can't follow your own project.", error.Message);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_Throws_WhenTheFollowDoesNotExist()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var projectContext = database.CreateProjectContext();

        var project = TestDataFactory.CreateProject("owner-1");
        projectContext.Projects.Add(project);
        await projectContext.SaveChangesAsync();

        var service = new ProjectFollowService(projectContext);

        var error = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdatePreferencesAsync(project.Id.ToString(), "follower-1", new UpdateProjectFollowPreferencesDto
            {
                ReceiveInAppNotifications = false,
                ReceiveEmailNotifications = true
            }));

        Assert.Equal("Project follow not found.", error.Message);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_PersistsTheNewFollowPreferences()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var projectContext = database.CreateProjectContext();

        var project = TestDataFactory.CreateProject("owner-1");
        projectContext.Projects.Add(project);
        projectContext.ProjectFollows.Add(new ProjectFollow
        {
            ProjectId = project.Id,
            FollowerUserId = "follower-1"
        });
        await projectContext.SaveChangesAsync();

        var service = new ProjectFollowService(projectContext);

        var result = await service.UpdatePreferencesAsync(project.Id.ToString(), "follower-1", new UpdateProjectFollowPreferencesDto
        {
            ReceiveInAppNotifications = false,
            ReceiveEmailNotifications = false
        });

        Assert.True(result.IsFollowing);
        Assert.Equal(1, result.FollowersCount);
        Assert.False(result.ReceiveInAppNotifications);
        Assert.False(result.ReceiveEmailNotifications);
    }
}
