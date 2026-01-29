using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    JwtTokenService jwtTokenService) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly JwtTokenService _jwtTokenService = jwtTokenService;

    [HttpPost("register")]
    public async Task<IActionResult> Register(AuthDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

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
        var token = _jwtTokenService.CreateToken(user, roles);

        return Ok(new { accessToken = token });
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


    // [Authorize]
    // [HttpPost("logout")]
    // public async Task<IActionResult> Logout()
    // {
    //     var user = await _userManager.GetUserAsync(User);
    //     user.RefreshToken = null;
    //     await _userManager.UpdateAsync(user);

    //     Response.Cookies.Delete("refreshToken");

    //     return Ok();
    // }

}