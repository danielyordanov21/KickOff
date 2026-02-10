public class ProjectDto
{
    public required Guid Id;
    public required string Name;
    public required string Goal;
    public required string Description;
    public ProjectState State;
    public required string Owner;
    public ICollection<string> CollaboratorsIdP = [];
    public ICollection<string> Contacts = [];
    public string? ExtraInfo;
    public required DateTime StartDate;
    public DateTime EndDate;
    public Guid SettingsId;
}