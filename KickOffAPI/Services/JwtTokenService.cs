using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class JwtTokenService(IConfiguration config)
{
    private readonly IConfiguration _config = config;

    public string CreateToken(ApplicationUser user, IList<string> roles)
    {
        var keyValue = GetRequiredConfigurationValue("Jwt:Key");
        var issuer = GetRequiredConfigurationValue("Jwt:Issuer");
        var audience = GetRequiredConfigurationValue("Jwt:Audience");
        var expiresMinutesText = GetRequiredConfigurationValue("Jwt:ExpiresMinutes");

        if (!double.TryParse(expiresMinutesText, out var expiresMinutes))
            throw new InvalidOperationException("Jwt:ExpiresMinutes must be a valid number.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(string ipAddress)
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }

    private string GetRequiredConfigurationValue(string key)
    {
        var value = _config[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Configuration value '{key}' is required. Provide it via environment variables or dotnet user-secrets.");

        return value;
    }
}
