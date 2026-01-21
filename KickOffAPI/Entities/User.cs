using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public required Guid Id { get; set; }
    
    [Required]
    public required Guid IdP { get; set; }

    [Required]
    public required UserRole Role { get; set; }

    [Required]
    public required string Username { get; set; }
    
    [Required]
    public required string PasswordHash { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public ICollection<string> ProjectIds { get; } = [];
    
    public ICollection<string> FollowerIdsP { get; } = [];
    public ICollection<string> FollowingIdsP { get; } = [];

    public UserState State { get; set; } = UserState.Unknown;
}