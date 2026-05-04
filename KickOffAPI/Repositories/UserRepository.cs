using Microsoft.EntityFrameworkCore;

public class UserRepository(AppIdentityDbContext context)
    : BaseRepository<ApplicationUser, string>(context)
{
    private readonly AppIdentityDbContext _context = context;
    private const string ProducerRoleNormalizedName = "PRODUCER";

    public async Task<ApplicationUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PublicId == publicId, ct);
    }

    public async Task<List<ApplicationUser>> GetRandomProducersAsync(int count, CancellationToken ct = default)
    {
        var take = Math.Clamp(count, 0, 20);

        if (take == 0)
        {
            return [];
        }

        return await (
            from user in _context.Users
            join userRole in _context.UserRoles on user.Id equals userRole.UserId
            join role in _context.Roles on userRole.RoleId equals role.Id
            where role.NormalizedName == ProducerRoleNormalizedName
            orderby Guid.NewGuid()
            select user)
            .Distinct()
            .AsNoTracking()
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<List<ApplicationUser>> SearchUsersAsync(string query, CancellationToken ct = default)
    {
        return await _context.Users
            .Where(x => x.UserName!.Contains(query))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<ApplicationUser>> GetFollowersAsync(string userId, CancellationToken ct = default)
    {
        return await _context.UserFollows
            .Where(x => x.FollowingId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Follower)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<ApplicationUser>> GetFollowingAsync(string userId, CancellationToken ct = default)
    {
        return await _context.UserFollows
            .Where(x => x.FollowerId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Following)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
