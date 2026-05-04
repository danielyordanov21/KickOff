using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KickOffAPI.Tests.Infrastructure;

public static class IdentityTestHarness
{
    public static UserManager<ApplicationUser> CreateUserManager(AppIdentityDbContext context)
    {
        var store = new UserStore<ApplicationUser, IdentityRole, AppIdentityDbContext>(context);

        return new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()],
            [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    public static RoleManager<IdentityRole> CreateRoleManager(AppIdentityDbContext context)
    {
        var store = new RoleStore<IdentityRole, AppIdentityDbContext>(context);

        return new RoleManager<IdentityRole>(
            store,
            [new RoleValidator<IdentityRole>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<IdentityRole>>.Instance);
    }

    public static SignInManager<ApplicationUser> CreateSignInManager(UserManager<ApplicationUser> userManager)
    {
        var identityOptions = Options.Create(new IdentityOptions());

        return new SignInManager<ApplicationUser>(
            userManager,
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext()
            },
            new UserClaimsPrincipalFactory<ApplicationUser>(userManager, identityOptions),
            identityOptions,
            NullLogger<SignInManager<ApplicationUser>>.Instance,
            new AuthenticationSchemeProvider(Options.Create(new AuthenticationOptions())),
            new TestUserConfirmation());
    }

    public static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager, params string[] roles)
    {
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                Assert.True(result.Succeeded, FormatErrors(result.Errors));
            }
        }
    }

    public static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        bool emailConfirmed = true,
        params string[] roles)
    {
        var user = new ApplicationUser
        {
            UserName = email.Split('@')[0],
            Email = email,
            EmailConfirmed = emailConfirmed
        };

        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, FormatErrors(createResult.Errors));

        if (roles.Length > 0)
        {
            var addToRoleResult = await userManager.AddToRolesAsync(user, roles);
            Assert.True(addToRoleResult.Succeeded, FormatErrors(addToRoleResult.Errors));
        }

        return await userManager.FindByIdAsync(user.Id)
            ?? throw new InvalidOperationException("Expected the created user to be reloadable.");
    }

    public static string FormatErrors(IEnumerable<IdentityError> errors)
    {
        return string.Join(" | ", errors.Select(error => error.Description));
    }

    private sealed class TestUserConfirmation : IUserConfirmation<ApplicationUser>
    {
        public Task<bool> IsConfirmedAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            return Task.FromResult(user.EmailConfirmed);
        }
    }
}
