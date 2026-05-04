using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.PublicId)
            .IsUnique();

        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);


        // UserFollow composite key
        builder.Entity<UserFollow>()
            .HasKey(x => new { x.FollowerId, x.FollowingId });

        builder.Entity<UserFollow>()
            .HasOne(x => x.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(x => x.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserFollow>()
            .HasOne(x => x.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(x => x.FollowingId)
            .OnDelete(DeleteBehavior.Restrict);

        // Useful indexes
        builder.Entity<UserFollow>()
            .HasIndex(x => x.FollowingId);

        builder.Entity<UserFollow>()
            .HasIndex(x => x.FollowerId);
    }
}
