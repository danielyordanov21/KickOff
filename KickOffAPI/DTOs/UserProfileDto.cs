namespace KickOffAPI.DTOs
{
    public class UserProfileDto : UserDto
    {
        public List<ProjectCatalogueDto> Projects { get; set; } = [];
        public List<ProjectCatalogueDto> BackedProjects { get; set; } = [];
        public List<ProfileConnectionDto> Followers { get; set; } = [];
        public List<ProfileConnectionDto> Following { get; set; } = [];
        public List<string> ProjectIds { get; set; } = [];
        public List<string> FollowerIdsP { get; set; } = [];
        public List<string> FollowingIdsP { get; set; } = [];
    }
}
