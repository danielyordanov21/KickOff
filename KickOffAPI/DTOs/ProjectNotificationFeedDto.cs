public class ProjectNotificationFeedDto
{
    public List<ProjectNotificationDto> Notifications { get; set; } = [];
    public int UnreadCount { get; set; }
}
