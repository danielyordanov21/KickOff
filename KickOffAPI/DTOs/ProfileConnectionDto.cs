namespace KickOffAPI.DTOs
{
    public class ProfileConnectionDto
    {
        public string Id { get; set; } = string.Empty;
        public string IdP { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string State { get; set; } = UserState.Unknown.ToString();
    }
}
