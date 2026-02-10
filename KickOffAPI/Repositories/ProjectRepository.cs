using Microsoft.EntityFrameworkCore;

public class ProjectRepository(ProjectDbContext context) : BaseRepository<Project>(context)
{
    private readonly ProjectDbContext _context = context;

    // Convenience: filter by enum value
    public async Task<List<Project>> FilterByStateAsync(ProjectState state)
    {
        return await _context.Projects.Where(p => p.State == state).ToListAsync();
    }

    // Backwards-compatible overload: accept string state and parse to enum
    public async Task<List<Project>> FilterByStateAsync(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("state is required", nameof(state));

        if (!Enum.TryParse<ProjectState>(state, ignoreCase: true, out var parsed))
            throw new ArgumentException($"Invalid project state: {state}", nameof(state));

        return await FilterByStateAsync(parsed);
    }
}