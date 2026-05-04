namespace KickOffAPI.DTOs
{
    public class DiscoverPersonDto
    {
        public Guid PublicId { get; set; }
        public required string Username { get; set; }
        public required string ProfilePictureUrl { get; set; }
    }
}
