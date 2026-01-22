using Microsoft.EntityFrameworkCore;

public class ProjectDbContext : BaseDbContext<ProjectDbContext>
{
    public DbSet<Project> Projects => Set<Project>();

    public ProjectDbContext(DbContextOptions<ProjectDbContext> options)
        : base(options)
    {
    }
}
