using System.ComponentModel.DataAnnotations;

public class ProjectNotification
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? ProjectUpdateId { get; set; }
    public ProjectUpdate? ProjectUpdate { get; set; }

    [Required]
    public string RecipientUserId { get; set; } = null!;

    [Required]
    [StringLength(160)]
    public string Title { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
