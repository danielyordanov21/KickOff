using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public Guid IdP { get; set; } = Guid.NewGuid();
    public UserState State { get; set; } = UserState.Unknown;
    public string? ProfilePictureUrl { get; set; }

    public ICollection<string> ProjectIds { get; } = [];
    public ICollection<string> FollowerIdsP { get; } = [];
    public ICollection<string> FollowingIdsP { get; } = [];
}