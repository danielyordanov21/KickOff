using KickOffAPI.Services;
using KickOffAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KickOffAPI.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ReturnsSuccessAndSessionTokens_ForValidCredentials()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var identityContext = database.CreateIdentityContext();

        var roleManager = IdentityTestHarness.CreateRoleManager(identityContext);
        await IdentityTestHarness.EnsureRolesAsync(roleManager, "User");

        var userManager = IdentityTestHarness.CreateUserManager(identityContext);
        await IdentityTestHarness.CreateUserAsync(userManager, "member@example.test", "Pass123$", roles: ["User"]);

        var service = CreateAuthService(identityContext, userManager);

        var result = await service.LoginAsync(new AuthDto
        {
            Email = "member@example.test",
            Password = "Pass123$"
        }, "127.0.0.1");

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.NotNull(result.Tokens);
        Assert.False(string.IsNullOrWhiteSpace(result.Tokens!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Tokens.RefreshToken.Token));
    }

    [Fact]
    public async Task LoginAsync_ReturnsAccountDeactivated_ForLongTermLockedUsers()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var identityContext = database.CreateIdentityContext();

        var roleManager = IdentityTestHarness.CreateRoleManager(identityContext);
        await IdentityTestHarness.EnsureRolesAsync(roleManager, "User");

        var userManager = IdentityTestHarness.CreateUserManager(identityContext);
        var user = await IdentityTestHarness.CreateUserAsync(userManager, "deactivated@example.test", "Pass123$", roles: ["User"]);

        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(60);
        var updateResult = await userManager.UpdateAsync(user);
        Assert.True(updateResult.Succeeded, IdentityTestHarness.FormatErrors(updateResult.Errors));

        var service = CreateAuthService(identityContext, userManager);

        var result = await service.LoginAsync(new AuthDto
        {
            Email = "deactivated@example.test",
            Password = "Pass123$"
        }, "127.0.0.1");

        Assert.Equal(LoginStatus.AccountDeactivated, result.Status);
        Assert.Equal("account_deactivated", result.Code);
        Assert.Contains("deactivated", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshTokenAsync_RotatesTheRefreshToken_WhenTheCookieValueIsValid()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var identityContext = database.CreateIdentityContext();

        var roleManager = IdentityTestHarness.CreateRoleManager(identityContext);
        await IdentityTestHarness.EnsureRolesAsync(roleManager, "User");

        var userManager = IdentityTestHarness.CreateUserManager(identityContext);
        var user = await IdentityTestHarness.CreateUserAsync(userManager, "member@example.test", "Pass123$", roles: ["User"]);

        var jwtTokenService = new JwtTokenService(TestServiceFactory.CreateConfiguration());
        var refreshToken = jwtTokenService.GenerateRefreshToken("127.0.0.1");
        user.RefreshTokens.Add(refreshToken);

        var updateResult = await userManager.UpdateAsync(user);
        Assert.True(updateResult.Succeeded, IdentityTestHarness.FormatErrors(updateResult.Errors));

        var service = CreateAuthService(identityContext, userManager, jwtTokenService);

        var result = await service.RefreshTokenAsync(refreshToken.Token, "127.0.0.1");
        var reloadedUser = await identityContext.Users
            .Include(existingUser => existingUser.RefreshTokens)
            .SingleAsync(existingUser => existingUser.Id == user.Id);

        Assert.Equal(RefreshSessionStatus.Success, result.Status);
        Assert.NotNull(result.Tokens);
        Assert.False(string.IsNullOrWhiteSpace(result.Tokens!.AccessToken));
        Assert.NotEqual(refreshToken.Token, result.Tokens.RefreshToken.Token);
        Assert.Equal(2, reloadedUser.RefreshTokens.Count);
        Assert.Single(reloadedUser.RefreshTokens, token => token.IsActive);
        Assert.NotNull(reloadedUser.RefreshTokens.Single(token => token.Token == refreshToken.Token).Revoked);
    }

    private static AuthService CreateAuthService(
        AppIdentityDbContext identityContext,
        Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
        JwtTokenService? tokenService = null)
    {
        return new AuthService(
            userManager,
            identityContext,
            IdentityTestHarness.CreateSignInManager(userManager),
            tokenService ?? new JwtTokenService(TestServiceFactory.CreateConfiguration()),
            null!,
            null!,
            new TestEmailService(),
            TestServiceFactory.CreateClientAppUrlResolver(authClientBaseUrl: "https://client.test"),
            new TestWebHostEnvironment("Development"),
            TestServiceFactory.CreateLogger<AuthService>());
    }
}
