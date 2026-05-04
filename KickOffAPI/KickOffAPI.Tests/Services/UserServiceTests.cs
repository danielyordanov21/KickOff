using KickOffAPI.DTOs;
using KickOffAPI.Services;
using KickOffAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KickOffAPI.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task UpdateChatPreferencesAsync_RejectsUnsupportedLanguages()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var identityContext = database.CreateIdentityContext();
        await using var projectContext = database.CreateProjectContext();

        var userManager = IdentityTestHarness.CreateUserManager(identityContext);
        var user = await IdentityTestHarness.CreateUserAsync(userManager, "member@example.test", "Pass123$");

        var service = CreateUserService(identityContext, projectContext, userManager);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateChatPreferencesAsync(user.Id, new UpdateUserChatPreferencesDto
            {
                PreferredChatLanguage = "pl",
                ShowOriginalChatTextByDefault = true
            }));

        Assert.Contains("supported chat languages", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAccountAsync_RejectsUsersWhoStillOwnProjects()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var identityContext = database.CreateIdentityContext();
        await using var projectContext = database.CreateProjectContext();

        var userManager = IdentityTestHarness.CreateUserManager(identityContext);
        var user = await IdentityTestHarness.CreateUserAsync(userManager, "owner@example.test", "Pass123$");

        projectContext.Projects.Add(TestDataFactory.CreateProject(user.Id));
        await projectContext.SaveChangesAsync();

        var service = CreateUserService(identityContext, projectContext, userManager);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAccountAsync(user.Id));

        Assert.Contains("still owns projects", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAccountAsync_RemovesTheUserAndRelatedFollowRecords_WhenDeletionIsAllowed()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var identityContext = database.CreateIdentityContext();
        await using var projectContext = database.CreateProjectContext();

        var userManager = IdentityTestHarness.CreateUserManager(identityContext);
        var user = await IdentityTestHarness.CreateUserAsync(userManager, "member@example.test", "Pass123$");
        var otherUser = await IdentityTestHarness.CreateUserAsync(userManager, "other@example.test", "Pass123$");
        var projectOwner = await IdentityTestHarness.CreateUserAsync(userManager, "project-owner@example.test", "Pass123$");

        identityContext.UserFollows.Add(new UserFollow
        {
            FollowerId = user.Id,
            FollowingId = otherUser.Id
        });

        var project = TestDataFactory.CreateProject(projectOwner.Id);
        projectContext.Projects.Add(project);
        projectContext.ProjectFollows.Add(new ProjectFollow
        {
            ProjectId = project.Id,
            FollowerUserId = user.Id
        });
        projectContext.ProjectNotifications.Add(new ProjectNotification
        {
            ProjectId = project.Id,
            RecipientUserId = user.Id,
            Title = "New update",
            Message = "A project update was published."
        });

        await identityContext.SaveChangesAsync();
        await projectContext.SaveChangesAsync();

        var service = CreateUserService(identityContext, projectContext, userManager);

        await service.DeleteAccountAsync(user.Id);

        Assert.False(await identityContext.Users.AnyAsync(existingUser => existingUser.Id == user.Id));
        Assert.False(await identityContext.UserFollows.AnyAsync(existingFollow =>
            existingFollow.FollowerId == user.Id || existingFollow.FollowingId == user.Id));
        Assert.False(await projectContext.ProjectFollows.AnyAsync(existingFollow => existingFollow.FollowerUserId == user.Id));
        Assert.False(await projectContext.ProjectNotifications.AnyAsync(notification => notification.RecipientUserId == user.Id));
    }

    private static UserService CreateUserService(
        AppIdentityDbContext identityContext,
        ProjectDbContext projectContext,
        Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
    {
        return new UserService(
            userManager,
            new UserRepository(identityContext),
            identityContext,
            projectContext,
            TestServiceFactory.CreateBlobService(),
            TestServiceFactory.CreateSendbirdService(),
            new UserFollowService(identityContext),
            TestServiceFactory.CreateLogger<UserService>());
    }
}
