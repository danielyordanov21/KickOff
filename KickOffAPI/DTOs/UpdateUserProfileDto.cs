using System.ComponentModel.DataAnnotations;

namespace KickOffAPI.DTOs
{
    public class UpdateUserProfileDto
    {
        [MinLength(3)]
        [MaxLength(64)]
        public required string UserName { get; set; }
    }
}
