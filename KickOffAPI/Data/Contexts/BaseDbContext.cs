using Microsoft.EntityFrameworkCore;

public abstract class BaseDbContext<T>(DbContextOptions<T> options) : DbContext(options) where T : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(T).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
