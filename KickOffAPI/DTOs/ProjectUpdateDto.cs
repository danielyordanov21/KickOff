public class ProjectUpdateDto
{
    public required Guid Id { get; set; }
    public required Guid ProjectId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string AuthorUserId { get; set; }
    public required string AuthorName { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public bool IsEdited { get; set; }
}
