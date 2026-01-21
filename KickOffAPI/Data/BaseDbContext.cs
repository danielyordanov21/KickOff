using Microsoft.EntityFrameworkCore;

public abstract class BaseDbContext<T> : DbContext where T : DbContext
{
    protected BaseDbContext(DbContextOptions<T> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(T).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
