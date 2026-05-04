public class ProjectDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Headline { get; set; }
    public required string Goal { get; set; }
    public required string Description { get; set; }
    public ProjectState State { get; set; }
    public required string Owner { get; set; }
    public required string OwnerId { get; set; }
    public required Guid OwnerPublicId { get; set; }
    public string? Category { get; set; }
    public decimal? FinancialGoal { get; set; }
    public string? Problem { get; set; }

    public ICollection<string> CollaboratorsIdP { get; set; } = new List<string>();
    public ICollection<string> Contacts { get; set; } = new List<string>();

    public ICollection<string> ImageUrls { get; set; } = new List<string>();
    public ICollection<string> ImageBlobNames { get; set; } = new List<string>();
    public ICollection<string> Tags { get; set; } = new List<string>();
    public ICollection<string> BackerIds { get; set; } = new List<string>();
    public ICollection<ProjectUpdateDto> Updates { get; set; } = new List<ProjectUpdateDto>();
    public ProjectFollowDto Follow { get; set; } = new();

    public string? ExtraInfo { get; set; }
    public required DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid SettingsId { get; set; }
}
