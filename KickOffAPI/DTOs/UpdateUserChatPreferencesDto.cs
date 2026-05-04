using System.ComponentModel.DataAnnotations;

namespace KickOffAPI.DTOs
{
    public class UpdateUserChatPreferencesDto
    {
        [MaxLength(16)]
        public string? PreferredChatLanguage { get; set; }

        public bool ShowOriginalChatTextByDefault { get; set; }
    }
}
