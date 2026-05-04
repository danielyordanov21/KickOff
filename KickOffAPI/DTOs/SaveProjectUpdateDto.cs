using System.ComponentModel.DataAnnotations;

public class SaveProjectUpdateDto
{
    [Required]
    [StringLength(120, MinimumLength = 3)]
    public required string Title { get; set; }

    [Required]
    [StringLength(4000, MinimumLength = 10)]
    public required string Content { get; set; }
}
