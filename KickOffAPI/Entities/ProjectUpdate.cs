using System.ComponentModel.DataAnnotations;

public class ProjectUpdate
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    public required string AuthorUserId { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 3)]
    public required string Title { get; set; }

    [Required]
    [StringLength(4000, MinimumLength = 10)]
    public required string Content { get; set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Project? Project { get; set; }
}
