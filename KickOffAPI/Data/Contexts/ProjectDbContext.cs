using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

public class ProjectDbContext(DbContextOptions<ProjectDbContext> options) : BaseDbContext<ProjectDbContext>(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectUpdate> ProjectUpdates => Set<ProjectUpdate>();
    public DbSet<ProjectFollow> ProjectFollows => Set<ProjectFollow>();
    public DbSet<ProjectNotification> ProjectNotifications => Set<ProjectNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                  .ValueGeneratedOnAdd();

            // Required strings
            entity.Property(p => p.Goal)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(p => p.Description)
                  .IsRequired()
                  .HasMaxLength(5000);

            entity.Property(p => p.OwnerId)
                  .IsRequired();

            entity.Property(p => p.SettingsId)
                  .IsRequired();

            // Optional strings
            entity.Property(p => p.Headline)
                  .IsRequired(false);

            entity.Property(p => p.Category)
                  .IsRequired(false);

            entity.Property(p => p.Problem)
                  .IsRequired(false);

            entity.Property(p => p.ExtraInfo)
                  .IsRequired(false);

            // Nullable DateTime
            entity.Property(p => p.EndsAt)
                  .IsRequired(false);

            // Money precision
            entity.Property(p => p.FinancialGoal)
                  .HasPrecision(18, 2);

            entity.Property(p => p.FinancialRaised)
                  .HasPrecision(18, 2);

            // Enum as string
            entity.Property(p => p.State)
                  .HasConversion<string>()
                  .IsRequired();

            // Primitive collections stored as JSON
            var stringCollectionConverter = new ValueConverter<ICollection<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<ICollection<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            entity.Property(p => p.ImageUrls)
                  .HasConversion(stringCollectionConverter)
                  .HasColumnType("nvarchar(max)");

            entity.Property(p => p.Tags)
                  .HasConversion(stringCollectionConverter)
                  .HasColumnType("nvarchar(max)");

            entity.Property(p => p.CollaboratorsIdP)
                  .HasConversion(stringCollectionConverter)
                  .HasColumnType("nvarchar(max)");

            entity.Property(p => p.Contacts)
                  .HasConversion(stringCollectionConverter)
                  .HasColumnType("nvarchar(max)");

            entity.Property(p => p.BackerIds)
                  .HasConversion(stringCollectionConverter)
                  .HasColumnType("nvarchar(max)");

            entity.HasMany(p => p.Updates)
                  .WithOne(update => update.Project)
                  .HasForeignKey(update => update.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Followers)
                  .WithOne(follow => follow.Project)
                  .HasForeignKey(follow => follow.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Notifications)
                  .WithOne(notification => notification.Project)
                  .HasForeignKey(notification => notification.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectUpdate>(entity =>
        {
            entity.HasKey(update => update.Id);

            entity.Property(update => update.Id)
                  .ValueGeneratedOnAdd();

            entity.Property(update => update.ProjectId)
                  .IsRequired();

            entity.Property(update => update.AuthorUserId)
                  .IsRequired();

            entity.Property(update => update.Title)
                  .IsRequired()
                  .HasMaxLength(120);

            entity.Property(update => update.Content)
                  .IsRequired()
                  .HasMaxLength(4000);

            entity.Property(update => update.CreatedAt)
                  .IsRequired();

            entity.Property(update => update.UpdatedAt)
                  .IsRequired();
        });

        modelBuilder.Entity<ProjectFollow>(entity =>
        {
            entity.HasKey(follow => new { follow.ProjectId, follow.FollowerUserId });

            entity.Property(follow => follow.FollowerUserId)
                  .IsRequired();

            entity.Property(follow => follow.ReceiveInAppNotifications)
                  .IsRequired();

            entity.Property(follow => follow.ReceiveEmailNotifications)
                  .IsRequired();

            entity.Property(follow => follow.CreatedAt)
                  .IsRequired();

            entity.HasIndex(follow => follow.FollowerUserId);
        });

        modelBuilder.Entity<ProjectNotification>(entity =>
        {
            entity.HasKey(notification => notification.Id);

            entity.Property(notification => notification.Id)
                  .ValueGeneratedOnAdd();

            entity.Property(notification => notification.RecipientUserId)
                  .IsRequired();

            entity.Property(notification => notification.Title)
                  .IsRequired()
                  .HasMaxLength(160);

            entity.Property(notification => notification.Message)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(notification => notification.CreatedAt)
                  .IsRequired();

            entity.Property(notification => notification.ReadAt)
                  .IsRequired(false);

            entity.HasIndex(notification => new
            {
                notification.RecipientUserId,
                notification.IsRead,
                notification.CreatedAt
            });

            entity.HasOne(notification => notification.ProjectUpdate)
                  .WithMany()
                  .HasForeignKey(notification => notification.ProjectUpdateId)
                  .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
