using System.ComponentModel.DataAnnotations;

namespace KickOffAPI.DTOs
{
    public class ChangeEmailDto
    {
        [EmailAddress]
        [MaxLength(256)]
        public required string NewEmail { get; set; }

        public required string CurrentPassword { get; set; }
    }
}
