using System.ComponentModel.DataAnnotations;

public class Project
{
    [Key]
    public required Guid Id { get; init; }

    [Required]
    [Length(10, 200)]
    public required string Goal { get; init; }

    [Required]
    [Length(20, 5000)]
    public required string Description { get; init; }

    [Required]
    public required string Owner { get; init; }

    public ICollection<string> CollaboratorsIdP { get; } = [];

    public ICollection<string> Contacts { get; } = [];

    public string? ExtraInfo { get; set; }

    [Required]
    public ProjectState State { get; set; } = ProjectState.Inactive;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public Guid SettingsId { get; set; }
}
