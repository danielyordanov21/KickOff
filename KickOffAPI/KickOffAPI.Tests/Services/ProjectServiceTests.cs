using KickOffAPI.Tests.Infrastructure;
using KickOffAPI.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace KickOffAPI.Tests.Services;

public class ProjectServiceTests
{
    [Fact]
    public async Task CreateAsync_RejectsUsersWithoutTheProducerOrAdminRole()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var identityContext = database.CreateIdentityContext();
        await using var projectContext = database.CreateProjectContext();

        var roleManager = IdentityTestHarness.CreateRoleManager(identityContext);
        await IdentityTestHarness.EnsureRolesAsync(roleManager, "User", "Producer", "Admin");

        var userManager = IdentityTestHarness.CreateUserManager(identityContext);
        var user = await IdentityTestHarness.CreateUserAsync(userManager, "member@example.test", "Pass123$", roles: ["User"]);

        var service = CreateProjectService(identityContext, projectContext, userManager, new TestEmailService());

        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(TestDataFactory.CreateProjectDto(), user.Id));

        Assert.Equal("Only producers and admins can create projects.", error.Message);
    }

    [Fact]
    public async Task CreateUpdateAsync_PersistsTheUpdate_AndCreatesNotifications()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var identityContext = database.CreateIdentityContext();
        await using var projectContext = database.CreateProjectContext();

        var userManager = IdentityTestHarness.CreateUserManager(identityContext);

        identityContext.Users.AddRange(
            new ApplicationUser { Id = "owner-1", UserName = "owner", Email = "owner@example.test" },
            new ApplicationUser { Id = "follower-1", UserName = "follower", Email = "follower@example.test" });

        var project = TestDataFactory.CreateProject("owner-1");
        projectContext.Projects.Add(project);
        projectContext.ProjectFollows.Add(new ProjectFollow
        {
            ProjectId = project.Id,
            FollowerUserId = "follower-1"
        });

        await identityContext.SaveChangesAsync();
        await projectContext.SaveChangesAsync();

        var emailService = new TestEmailService();
        var service = CreateProjectService(identityContext, projectContext, userManager, emailService);

        var result = await service.CreateUpdateAsync(
            project.Id.ToString(),
            TestDataFactory.CreateSaveProjectUpdateDto(),
            "owner-1");

        Assert.Equal("owner-1", result.AuthorUserId);
        Assert.Equal("owner", result.AuthorName);
        Assert.Equal(1, await projectContext.ProjectUpdates.CountAsync());
        Assert.Equal(1, await projectContext.ProjectNotifications.CountAsync());
        Assert.Single(emailService.SentEmails);
    }

    [Fact]
    public async Task DeleteProjectUpdateAsync_RemovesTheUpdate_AndClearsNotificationReferences()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var identityContext = database.CreateIdentityContext();
        await using var projectContext = database.CreateProjectContext();

        var userManager = IdentityTestHarness.CreateUserManager(identityContext);

        identityContext.Users.Add(new ApplicationUser
        {
            Id = "owner-1",
            UserName = "owner",
            Email = "owner@example.test"
        });

        var project = TestDataFactory.CreateProject("owner-1");
        var update = TestDataFactory.CreateProjectUpdate(project.Id, "owner-1");
        var notification = new ProjectNotification
        {
            ProjectId = project.Id,
            ProjectUpdateId = update.Id,
            RecipientUserId = "recipient-1",
            Title = "New update",
            Message = "A project update was published."
        };

        projectContext.Projects.Add(project);
        projectContext.ProjectUpdates.Add(update);
        projectContext.ProjectNotifications.Add(notification);

        await identityContext.SaveChangesAsync();
        await projectContext.SaveChangesAsync();

        var service = CreateProjectService(identityContext, projectContext, userManager, new TestEmailService());

        await service.DeleteProjectUpdateAsync(project.Id.ToString(), update.Id.ToString(), "owner-1");

        Assert.Empty(projectContext.ProjectUpdates);
        Assert.Null(await projectContext.ProjectNotifications.Select(existingNotification => existingNotification.ProjectUpdateId).SingleAsync());
    }

    [Fact]
    public async Task GetProjectsAsync_SortsByNewestFirst_AndReturnsPaginationMetadata()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var identityContext = database.CreateIdentityContext();
        await using var projectContext = database.CreateProjectContext();

        var userManager = IdentityTestHarness.CreateUserManager(identityContext);

        identityContext.Users.Add(new ApplicationUser
        {
            Id = "owner-1",
            UserName = "owner",
            Email = "owner@example.test"
        });

        var oldestProject = TestDataFactory.CreateProject("owner-1", goal: "Oldest project goal");
        var middleProject = TestDataFactory.CreateProject("owner-1", goal: "Middle project goal");
        var newestProject = TestDataFactory.CreateProject("owner-1", goal: "Newest project goal");

        projectContext.Projects.AddRange(oldestProject, middleProject, newestProject);
        projectContext.Entry(oldestProject).Property(nameof(Project.CreatedAt)).CurrentValue = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        projectContext.Entry(middleProject).Property(nameof(Project.CreatedAt)).CurrentValue = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc);
        projectContext.Entry(newestProject).Property(nameof(Project.CreatedAt)).CurrentValue = new DateTime(2026, 03, 01, 0, 0, 0, DateTimeKind.Utc);

        await identityContext.SaveChangesAsync();
        await projectContext.SaveChangesAsync();

        var service = CreateProjectService(identityContext, projectContext, userManager, new TestEmailService());

        var result = await service.GetProjectsAsync(pageNumber: 1, pageSize: 2, sortNewest: true);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Equal([newestProject.Id, middleProject.Id], result.Data.Select(project => project.Id));
    }

    private static ProjectService CreateProjectService(
        AppIdentityDbContext identityContext,
        ProjectDbContext projectContext,
        Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
        TestEmailService emailService)
    {
        var projectRepository = new ProjectRepository(projectContext);
        var notificationService = new ProjectNotificationService(
            projectContext,
            identityContext,
            emailService,
            TestServiceFactory.CreateClientAppUrlResolver(authClientBaseUrl: "https://client.test"),
            TestServiceFactory.CreateLogger<ProjectNotificationService>());

        return new ProjectService(
            projectRepository,
            userManager,
            identityContext,
            projectContext,
            TestServiceFactory.CreateBlobService(),
            notificationService,
            new CacheService(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()))),
            TestServiceFactory.CreateLogger<ProjectService>());
    }
}
