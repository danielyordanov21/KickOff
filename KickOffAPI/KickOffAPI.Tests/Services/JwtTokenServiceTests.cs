using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using KickOffAPI.Tests.Infrastructure;

namespace KickOffAPI.Tests.Services;

public class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_EmbedsExpectedUserClaims()
    {
        var configuration = TestServiceFactory.CreateConfiguration();
        var service = new JwtTokenService(configuration);
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "user@example.test"
        };

        var tokenText = service.CreateToken(user, ["Producer", "Admin"]);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenText);

        Assert.Equal("user-1", token.Subject);
        Assert.Equal("user@example.test", token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Producer");
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Admin");
    }

    [Fact]
    public void CreateToken_Throws_WhenExpirationMinutesAreInvalid()
    {
        var configuration = TestServiceFactory.CreateConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:ExpiresMinutes"] = "not-a-number"
        });
        var service = new JwtTokenService(configuration);
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "user@example.test"
        };

        var error = Assert.Throws<InvalidOperationException>(() => service.CreateToken(user, []));

        Assert.Equal("Jwt:ExpiresMinutes must be a valid number.", error.Message);
    }
}
