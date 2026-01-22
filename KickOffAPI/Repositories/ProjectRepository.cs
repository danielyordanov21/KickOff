public class ProjectRepository : BaseRepository<Project>
{
    public ProjectRepository(ProjectDbContext context) : base(context)
    {
    }
}