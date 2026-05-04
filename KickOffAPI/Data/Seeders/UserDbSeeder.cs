using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KickOffAPI.Data.Seeders;

public static class UserDbSeeder
{
    private const string AdminEmail = "admin@kickoff.test";

    private static readonly SeedUser[] SeedUsers =
    [
        new("maya.ross", "maya.ross@kickoff.test", "Producer", UserState.Online, 180),
        new("eli.turner", "eli.turner@kickoff.test", "User", UserState.Away, 165),
        new("nora.bennett", "nora.bennett@kickoff.test", "User", UserState.Busy, 150),
        new("samir.khan", "samir.khan@kickoff.test", "Producer", UserState.Online, 140),
        new("zoe.owens", "zoe.owens@kickoff.test", "User", UserState.Offline, 132),
        new("leah.cole", "leah.cole@kickoff.test", "User", UserState.Online, 120),
        new("marcus.reed", "marcus.reed@kickoff.test", "Producer", UserState.Busy, 110),
        new("amina.hassan", "amina.hassan@kickoff.test", "User", UserState.Away, 104),
        new("jonah.price", "jonah.price@kickoff.test", "User", UserState.Online, 96),
        new("riley.park", "riley.park@kickoff.test", "User", UserState.Unknown, 88),
        new("ivy.morgan", "ivy.morgan@kickoff.test", "Producer", UserState.Online, 76),
        new("noah.brooks", "noah.brooks@kickoff.test", "User", UserState.Offline, 64),
        new("lina.stone", "lina.stone@kickoff.test", "User", UserState.Busy, 56),
        new("darius.wells", "darius.wells@kickoff.test", "Producer", UserState.Away, 48),
        new("talia.grant", "talia.grant@kickoff.test", "User", UserState.Online, 36)
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var adminPassword = GetRequiredDevelopmentSeedValue(configuration, "DevelopmentSeed:AdminPassword");
        var userPassword = GetRequiredDevelopmentSeedValue(configuration, "DevelopmentSeed:UserPassword");

        await AdminAccountBootstrapper.EnsureAdminUserAsync(userManager, AdminEmail, adminPassword);

        if (await userManager.Users.AnyAsync())
        {
            var userCount = await userManager.Users.CountAsync();
            if (userCount > 1)
                return;
        }

        var now = DateTime.UtcNow;

        foreach (var seedUser in SeedUsers)
        {
            var user = new ApplicationUser
            {
                UserName = seedUser.UserName,
                Email = seedUser.Email,
                PublicId = Guid.NewGuid(),
                State = seedUser.State,
                CreatedAt = now.AddDays(-seedUser.CreatedDaysAgo),
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, userPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create user {user.UserName}: {errors}");
            }

            var roleResult = await userManager.AddToRoleAsync(user, seedUser.Role);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                throw new Exception($"Failed to assign {seedUser.Role} role to {user.UserName}: {errors}");
            }
        }
    }

    private static string GetRequiredDevelopmentSeedValue(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Configuration value '{key}' is required for development seed data. Provide it via environment variables or dotnet user-secrets.");

        return value;
    }

    private sealed record SeedUser(
        string UserName,
        string Email,
        string Role,
        UserState State,
        int CreatedDaysAgo);
}
