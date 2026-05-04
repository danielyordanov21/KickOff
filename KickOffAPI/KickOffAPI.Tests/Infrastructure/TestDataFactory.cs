namespace KickOffAPI.Tests.Infrastructure;

public static class TestDataFactory
{
    public static CreateProjectDto CreateProjectDto(
        string goal = "Build a trusted creator matching network",
        string description = "This project description is intentionally long enough to satisfy validation rules.",
        ProjectState state = ProjectState.Active)
    {
        return new CreateProjectDto
        {
            Goal = goal,
            Description = description,
            State = state,
            SettingsId = Guid.NewGuid()
        };
    }

    public static Project CreateProject(
        string ownerId,
        Guid? id = null,
        string? headline = "KickOff Test Project",
        string goal = "Build a trusted creator matching network",
        string description = "This project description is intentionally long enough to satisfy validation rules.",
        ProjectState state = ProjectState.Active)
    {
        return new Project
        {
            Id = id ?? Guid.NewGuid(),
            Headline = headline,
            Goal = goal,
            Description = description,
            OwnerId = ownerId,
            State = state,
            SettingsId = Guid.NewGuid()
        };
    }

    public static SaveProjectUpdateDto CreateSaveProjectUpdateDto(
        string title = "Milestone reached",
        string content = "We have enough context here to satisfy the minimum update length.")
    {
        return new SaveProjectUpdateDto
        {
            Title = title,
            Content = content
        };
    }

    public static ProjectUpdate CreateProjectUpdate(
        Guid projectId,
        string authorUserId,
        string title = "Milestone reached",
        string content = "We have enough context here to satisfy the minimum update length.")
    {
        return new ProjectUpdate
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            AuthorUserId = authorUserId,
            Title = title,
            Content = content,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
