using Microsoft.EntityFrameworkCore;

public class UserDbContext : BaseDbContext<UserDbContext>
{
    public DbSet<User> Users => Set<User>();

    public UserDbContext(DbContextOptions<UserDbContext> options)
        : base(options)
    {
    }
}
