using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public UserState State { get; set; } = UserState.Unknown;
    public string? ProfilePictureUrl { get; set; }
    [MaxLength(16)]
    public string? PreferredChatLanguage { get; set; }
    public bool ShowOriginalChatTextByDefault { get; set; }
    public ICollection<string> ProjectIds { get; } = [];

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public ICollection<UserFollow> Followers { get; set; } = new List<UserFollow>();

    public ICollection<UserFollow> Following { get; set; } = new List<UserFollow>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
