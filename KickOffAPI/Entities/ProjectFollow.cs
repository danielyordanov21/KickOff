using System.ComponentModel.DataAnnotations;

public class ProjectFollow
{
    [Required]
    public Guid ProjectId { get; set; }

    public Project? Project { get; set; }

    [Required]
    public string FollowerUserId { get; set; } = null!;

    public bool ReceiveInAppNotifications { get; set; } = true;
    public bool ReceiveEmailNotifications { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
