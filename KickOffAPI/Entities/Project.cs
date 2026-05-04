
using System.ComponentModel.DataAnnotations;

public class Project
{
    [Key]
    public Guid Id { get; set; }

    public string? Headline { get; set; }

    public ICollection<string> ImageUrls { get; set; } = new List<string>();

    public ICollection<string> Tags { get; set; } = new List<string>();

    public string? Category { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 10)]
    public required string Goal { get; set; }

    public decimal? FinancialGoal { get; set; }
    public decimal? FinancialRaised { get; set; }

    public string? Problem { get; set; }

    [Required]
    [StringLength(5000, MinimumLength = 20)]
    public required string Description { get; set; }

    [Required]
    public required string OwnerId { get; set; }

    public ICollection<string> CollaboratorsIdP { get; set; } = new List<string>();

    public ICollection<string> Contacts { get; set; } = new List<string>();

    public string? ExtraInfo { get; set; }

    [Required]
    public ProjectState State { get; set; } = ProjectState.Inactive;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndsAt { get; set; }

    [Required]
    public Guid SettingsId { get; set; }

    public ICollection<string> BackerIds { get; set; } = new List<string>();

    public ICollection<ProjectUpdate> Updates { get; set; } = new List<ProjectUpdate>();
    public ICollection<ProjectFollow> Followers { get; set; } = new List<ProjectFollow>();
    public ICollection<ProjectNotification> Notifications { get; set; } = new List<ProjectNotification>();
}
