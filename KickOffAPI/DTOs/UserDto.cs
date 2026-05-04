namespace KickOffAPI.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string IdP { get; set; } = string.Empty;
        public string? Role { get; set; }
        public List<string> Roles { get; set; } = [];
        public string State { get; set; } = UserState.Unknown.ToString();
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string? PreferredChatLanguage { get; set; }
        public bool ShowOriginalChatTextByDefault { get; set; }
        public bool? CanDeleteAccount { get; set; }
        public string? DeleteAccountRestriction { get; set; }
    }
}
