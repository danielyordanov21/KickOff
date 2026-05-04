public interface IUserFollowService
{
    Task FollowAsync(string followerId, string followingId);

    Task UnfollowAsync(string followerId, string followingId);

    Task<bool> IsFollowingAsync(string followerId, string followingId);

    Task<int> GetFollowersCountAsync(string userId);

    Task<int> GetFollowingCountAsync(string userId);
}
