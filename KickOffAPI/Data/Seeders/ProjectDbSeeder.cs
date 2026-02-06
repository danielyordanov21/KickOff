public class ProjectDbSeeder(ProjectRepository projectRepository)
{
    private readonly ProjectRepository _projectRepository = projectRepository;

    public async Task SeedAsync()
    {
        // Check if there are any projects already
        var existingProjects = await _projectRepository.GetAllAsync();
        if (existingProjects.Any())
        {
            return; // Database has been seeded
        }

        // Seed initial projects
        var projects = new List<Project>
        {
            new Project { Id = Guid.NewGuid(), Description = "Description for Project Alpha", Goal = "lorem ipsum ig", Owner = "owner1" },
            new Project { Id = Guid.NewGuid(), Description = "Description for Project Beta", Goal = "lorem ipsum dolor", Owner = "owner2" },
            new Project { Id = Guid.NewGuid(), Description = "Description for Project Gamma", Goal = "lorem ipsum amet", Owner = "owner3" },
        };

        await _projectRepository.AddRangeAsync(projects);
    }
}