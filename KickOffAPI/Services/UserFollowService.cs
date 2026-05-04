using Microsoft.EntityFrameworkCore;

public class UserFollowService : IUserFollowService
{
    private readonly AppIdentityDbContext _context;

    public UserFollowService(AppIdentityDbContext context)
    {
        _context = context;
    }

    public async Task FollowAsync(string followerId, string followingId)
    {
        if (followerId == followingId)
            return;

        var exists = await _context.UserFollows
            .AnyAsync(x => x.FollowerId == followerId &&
                           x.FollowingId == followingId);

        if (exists)
            return;

        _context.UserFollows.Add(new UserFollow
        {
            FollowerId = followerId,
            FollowingId = followingId
        });

        await _context.SaveChangesAsync();
    }

    public async Task UnfollowAsync(string followerId, string followingId)
    {
        var entity = await _context.UserFollows.FindAsync(followerId, followingId);

        if (entity == null)
            return;

        _context.UserFollows.Remove(entity);

        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsFollowingAsync(string followerId, string followingId)
    {
        return await _context.UserFollows
            .AnyAsync(x => x.FollowerId == followerId &&
                           x.FollowingId == followingId);
    }

    public async Task<int> GetFollowersCountAsync(string userId)
    {
        return await _context.UserFollows
            .CountAsync(x => x.FollowingId == userId);
    }

    public async Task<int> GetFollowingCountAsync(string userId)
    {
        return await _context.UserFollows
            .CountAsync(x => x.FollowerId == userId);
    }
}
