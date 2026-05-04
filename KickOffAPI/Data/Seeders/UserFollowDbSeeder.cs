using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KickOffAPI.Data.Seeders;

public static class UserFollowDbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var users = await userManager.Users.ToListAsync();

        if (await context.UserFollows.AnyAsync())
            return;

        var random = new Random();
        var now = DateTime.UtcNow;
        var followedPairs = new HashSet<(string FollowerId, string FollowingId)>();

        foreach (var user in users)
        {
            // Each user follows 3-5 other random users.
            int followCount = random.Next(3, 6);

            var followCandidates = users
                .Where(candidate => candidate.Id != user.Id)
                .OrderBy(_ => random.Next())
                .Take(followCount)
                .ToList();

            foreach (var target in followCandidates)
            {
                if (!followedPairs.Add((user.Id, target.Id)))
                    continue;

                var userFollow = new UserFollow
                {
                    FollowerId = user.Id,
                    FollowingId = target.Id,
                    CreatedAt = now
                        .AddDays(-random.Next(2, 90))
                        .AddMinutes(-random.Next(0, 1_440))
                };

                context.UserFollows.Add(userFollow);
            }
        }

        await context.SaveChangesAsync();
    }
}
