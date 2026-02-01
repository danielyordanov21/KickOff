using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class AuthController(UserManager<ApplicationUser> userManager,
                            AppIdentityDbContext context,
                            SignInManager<ApplicationUser> signInManager,
                            JwtTokenService tokenService) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly AppIdentityDbContext _context = context;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly JwtTokenService _tokenService = tokenService;

    [HttpPost("register")]
    public async Task<IActionResult> Register(AuthDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        await _userManager.AddToRoleAsync(user, "User");

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok("User created" + user.Id);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(AuthDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return Unauthorized("Invalid credentials");

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, true);

        if (result.IsLockedOut)
            return BadRequest("Account locked");

        if (!result.Succeeded)
            return Unauthorized("Invalid credentials");

        var roles = await _userManager.GetRolesAsync(user);

        var token = _tokenService.CreateToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(GetIpAddress());

        user.RefreshTokens.Add(refreshToken);
        await _userManager.UpdateAsync(user);

        SetRefreshTokenCookie(refreshToken);

        return Ok(new { accessToken = token });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (refreshToken == null) return Unauthorized();

        var user = _context.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == refreshToken));

        if (user == null) return Unauthorized();

        var token = user.RefreshTokens.Single(x => x.Token == refreshToken);

        if (!token.IsActive) return Unauthorized();

        var newRefreshToken = _tokenService.GenerateRefreshToken(GetIpAddress());
        token.Revoked = DateTime.UtcNow;
        token.ReplacedByToken = newRefreshToken.Token;
        user.RefreshTokens.Add(newRefreshToken);

        await _context.SaveChangesAsync();

        SetRefreshTokenCookie(newRefreshToken);

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.CreateToken(user, roles);

        return Ok(new { accessToken = newAccessToken });
    }


    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        var user = await _userManager.GetUserAsync(User);

        var token = user?.RefreshTokens.SingleOrDefault(t => t.Token == refreshToken);
        if (token != null)
        {
            token.Revoked = DateTime.UtcNow;
            token.RevokedByIp = GetIpAddress();
            await _context.SaveChangesAsync();
        }

        Response.Cookies.Delete("refreshToken");
        return Ok();
    }


    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.GetUserAsync(User);

        return Ok(new
        {
            user?.Id,
            user?.Email,
            user?.UserName
        });
    }


    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"].ToString();

        return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
    }

    private void SetRefreshTokenCookie(RefreshToken token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Strict,
            Expires = token.Expires
        };

        Response.Cookies.Append("refreshToken", token.Token, cookieOptions);
    }
}