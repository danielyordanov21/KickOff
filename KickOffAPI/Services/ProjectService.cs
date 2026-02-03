public class ProjectService(ProjectRepository ProjectRepository)
{
    private readonly ProjectRepository _ProjectRepository = ProjectRepository;

    public async Task<IEnumerable<Project>> GetCatalogueAsync()
    {
        Console.WriteLine("Fetching project catalogue...");
        var projects = await _ProjectRepository.GetAllAsync();
        System.Console.WriteLine($"Found {projects.Count()} projects");
        return projects;
    }
}