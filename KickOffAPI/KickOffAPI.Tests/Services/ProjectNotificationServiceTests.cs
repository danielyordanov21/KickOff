using KickOffAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KickOffAPI.Tests.Services;

public class ProjectNotificationServiceTests
{
    [Fact]
    public async Task NotifyProjectUpdatePublishedAsync_CreatesInAppNotifications_AndEmailsOptedInFollowers()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var identityContext = database.CreateIdentityContext();
        await using var projectContext = database.CreateProjectContext();

        identityContext.Users.AddRange(
            new ApplicationUser { Id = "owner-1", UserName = "owner", Email = "owner@example.test" },
            new ApplicationUser { Id = "follower-1", UserName = "follower1", Email = "follower1@example.test" },
            new ApplicationUser { Id = "follower-2", UserName = "follower2", Email = "follower2@example.test" });

        var project = TestDataFactory.CreateProject("owner-1");
        var update = TestDataFactory.CreateProjectUpdate(project.Id, "owner-1");

        projectContext.Projects.Add(project);
        projectContext.ProjectFollows.AddRange(
            new ProjectFollow
            {
                ProjectId = project.Id,
                FollowerUserId = "follower-1",
                ReceiveInAppNotifications = true,
                ReceiveEmailNotifications = true
            },
            new ProjectFollow
            {
                ProjectId = project.Id,
                FollowerUserId = "follower-2",
                ReceiveInAppNotifications = true,
                ReceiveEmailNotifications = false
            },
            new ProjectFollow
            {
                ProjectId = project.Id,
                FollowerUserId = "owner-1",
                ReceiveInAppNotifications = true,
                ReceiveEmailNotifications = true
            });
        await identityContext.SaveChangesAsync();
        await projectContext.SaveChangesAsync();

        var emailService = new TestEmailService();
        var resolver = TestServiceFactory.CreateClientAppUrlResolver(authClientBaseUrl: "https://client.test");
        var service = new ProjectNotificationService(
            projectContext,
            identityContext,
            emailService,
            resolver,
            TestServiceFactory.CreateLogger<ProjectNotificationService>());

        await service.NotifyProjectUpdatePublishedAsync(project, update);

        var notifications = await projectContext.ProjectNotifications
            .OrderBy(notification => notification.RecipientUserId)
            .ToListAsync();

        Assert.Equal(["follower-1", "follower-2"], notifications.Select(notification => notification.RecipientUserId));
        Assert.Single(emailService.SentEmails);
        Assert.Equal("follower1@example.test", emailService.SentEmails[0].ToAddress);
        Assert.Contains($"https://client.test/project/{project.Id}", emailService.SentEmails[0].Body);
    }

    [Fact]
    public async Task MarkAsReadAsync_SetsTheReadFlags()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var identityContext = database.CreateIdentityContext();
        await using var projectContext = database.CreateProjectContext();

        identityContext.Users.Add(new ApplicationUser
        {
            Id = "recipient-1",
            UserName = "recipient",
            Email = "recipient@example.test"
        });

        var project = TestDataFactory.CreateProject("owner-1");
        var notification = new ProjectNotification
        {
            ProjectId = project.Id,
            RecipientUserId = "recipient-1",
            Title = "New update from KickOff",
            Message = "The creator posted a new update."
        };

        projectContext.Projects.Add(project);
        projectContext.ProjectNotifications.Add(notification);
        await identityContext.SaveChangesAsync();
        await projectContext.SaveChangesAsync();

        var service = new ProjectNotificationService(
            projectContext,
            identityContext,
            new TestEmailService(),
            TestServiceFactory.CreateClientAppUrlResolver(authClientBaseUrl: "https://client.test"),
            TestServiceFactory.CreateLogger<ProjectNotificationService>());

        await service.MarkAsReadAsync(notification.Id.ToString(), "recipient-1");

        Assert.True(notification.IsRead);
        Assert.NotNull(notification.ReadAt);
    }
}
