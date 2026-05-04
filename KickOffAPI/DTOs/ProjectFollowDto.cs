public class ProjectFollowDto
{
    public int FollowersCount { get; set; }
    public bool IsFollowing { get; set; }
    public bool ReceiveInAppNotifications { get; set; } = true;
    public bool ReceiveEmailNotifications { get; set; } = true;
}
