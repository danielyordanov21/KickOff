using Microsoft.AspNetCore.Identity;

namespace KickOffAPI.Data.Seeders;

public static class AdminAccountBootstrapper
{
    private const string AdminRoleName = "Admin";

    public static async Task EnsureAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        string adminEmail,
        string adminPassword)
    {
        var normalizedEmail = adminEmail.Trim();
        var adminUser = await userManager.FindByEmailAsync(normalizedEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                PublicId = Guid.NewGuid(),
                State = UserState.Online,
                CreatedAt = DateTime.UtcNow.AddDays(-365),
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                throw new Exception($"Failed to create admin user {normalizedEmail}: {errors}");
            }
        }

        var roles = await userManager.GetRolesAsync(adminUser);
        if (!roles.Contains(AdminRoleName))
        {
            var roleResult = await userManager.AddToRoleAsync(adminUser, AdminRoleName);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(error => error.Description));
                throw new Exception($"Failed to assign {AdminRoleName} role to {normalizedEmail}: {errors}");
            }
        }

        var adminNeedsUpdate = false;

        if (!adminUser.EmailConfirmed)
        {
            adminUser.EmailConfirmed = true;
            adminNeedsUpdate = true;
        }

        if (adminUser.State != UserState.Online)
        {
            adminUser.State = UserState.Online;
            adminNeedsUpdate = true;
        }

        if (adminNeedsUpdate)
        {
            var updateResult = await userManager.UpdateAsync(adminUser);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(error => error.Description));
                throw new Exception($"Failed to update admin user {normalizedEmail}: {errors}");
            }
        }
    }
}
