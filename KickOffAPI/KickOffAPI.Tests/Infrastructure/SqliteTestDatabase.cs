using Microsoft.EntityFrameworkCore;

namespace KickOffAPI.Tests.Infrastructure;

public sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly string _identityDatabaseName;
    private readonly string _projectDatabaseName;
    private readonly DbContextOptions<AppIdentityDbContext> _identityOptions;
    private readonly DbContextOptions<ProjectDbContext> _projectOptions;

    private SqliteTestDatabase()
    {
        _identityDatabaseName = $"KickOffApiIdentityTests_{Guid.NewGuid():N}";
        _projectDatabaseName = $"KickOffApiProjectTests_{Guid.NewGuid():N}";

        _identityOptions = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseInMemoryDatabase(_identityDatabaseName)
            .Options;

        _projectOptions = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseInMemoryDatabase(_projectDatabaseName)
            .Options;
    }

    public static async Task<SqliteTestDatabase> CreateAsync()
    {
        var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        return database;
    }

    public AppIdentityDbContext CreateIdentityContext() => new(_identityOptions);

    public ProjectDbContext CreateProjectContext() => new(_projectOptions);

    private async Task InitializeAsync()
    {
        await using var identityContext = CreateIdentityContext();
        await identityContext.Database.EnsureCreatedAsync();

        await using var projectContext = CreateProjectContext();
        await projectContext.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await using var projectContext = CreateProjectContext();
        await projectContext.Database.EnsureDeletedAsync();

        await using var identityContext = CreateIdentityContext();
        await identityContext.Database.EnsureDeletedAsync();
    }
}
