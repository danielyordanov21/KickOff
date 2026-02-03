using Microsoft.EntityFrameworkCore;

public class ProjectDbContext(DbContextOptions<ProjectDbContext> options) : BaseDbContext<ProjectDbContext>(options)
{
    public DbSet<Project> Projects => Set<Project>();
}
