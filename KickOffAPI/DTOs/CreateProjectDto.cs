using System.ComponentModel.DataAnnotations;

public class CreateProjectDto
{
    public string? Headline { get; set; }

    public ICollection<string> ImageUrls { get; set; } = new List<string>();

    public ICollection<string> Tags { get; set; } = new List<string>();

    public string? Category { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 10)]
    public required string Goal { get; set; }

    public decimal? FinancialGoal { get; set; }

    public string? Problem { get; set; }

    [Required]
    [StringLength(5000, MinimumLength = 20)]
    public required string Description { get; set; }

    public ICollection<string> CollaboratorsIdP { get; set; } = new List<string>();

    public ICollection<string> Contacts { get; set; } = new List<string>();

    public string? ExtraInfo { get; set; }

    public ProjectState State { get; set; } = ProjectState.Inactive;

    public DateTime? EndsAt { get; set; }

    [Required]
    public Guid SettingsId { get; set; }
}
