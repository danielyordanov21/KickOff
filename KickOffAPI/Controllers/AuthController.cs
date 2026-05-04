using KickOffAPI.DTOs;
using KickOffAPI.Exceptions;
using KickOffAPI.Extensions;
using KickOffAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    AuthService authService,
    IWebHostEnvironment hostEnvironment) : ControllerBase
{
    private readonly AuthService _authService = authService;
    private readonly IWebHostEnvironment _hostEnvironment = hostEnvironment;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto, cancellationToken);

            return Ok(new
            {
                requiresEmailConfirmation = false,
                emailDeliveryEnabled = result.Confirmation.EmailDeliveryEnabled,
                verificationUrl = result.Confirmation.VerificationUrl,
                message = result.Confirmation.Message
            });
        }
        catch (IdentityOperationException ex)
        {
            return BadRequestIdentityErrors(ex.Errors, ex.Message);
        }
        catch (UpstreamDependencyException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = ex.Message,
                detail = ex.Detail
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(AuthDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(dto, Request.GetClientIpAddress(), cancellationToken);

        switch (result.Status)
        {
            case LoginStatus.Success:
                SetRefreshTokenCookie(result.Tokens!.RefreshToken);
                return Ok(new { accessToken = result.Tokens.AccessToken });

            case LoginStatus.AccountDeactivated:
            case LoginStatus.AccountLocked:
            case LoginStatus.SignInNotAllowed:
                return BadRequestMessage(result.Message!, result.Code!);

            default:
                return Unauthorized("Invalid credentials");
        }
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new
            {
                success = false,
                message = "The email confirmation link is incomplete."
            });
        }

        var result = await _authService.ConfirmEmailAsync(userId, code);

        return result.Status switch
        {
            ConfirmEmailStatus.NotFound => NotFound(new
            {
                success = false,
                message = result.Message
            }),
            ConfirmEmailStatus.InvalidLink => BadRequest(new
            {
                success = false,
                message = result.Message
            }),
            ConfirmEmailStatus.AlreadyConfirmed => Ok(new
            {
                success = true,
                alreadyConfirmed = true,
                message = result.Message
            }),
            _ => Ok(new
            {
                success = true,
                alreadyConfirmed = false,
                message = result.Message
            })
        };
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(
        ResendEmailConfirmationDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.ResendConfirmationAsync(dto, cancellationToken);

            return Ok(new
            {
                alreadyConfirmed = result.AlreadyConfirmed,
                emailDeliveryEnabled = result.EmailDeliveryEnabled,
                verificationUrl = result.VerificationUrl,
                message = result.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            await _authService.RequestPasswordResetAsync(dto, cancellationToken);
            return Ok(new
            {
                message = "If an account exists for that email, we sent password reset instructions."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(
            dto,
            Request.GetClientIpAddress(),
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto, CancellationToken cancellationToken)
    {
        var userId = User.GetApplicationUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        try
        {
            var result = await _authService.ChangePasswordAsync(
                userId,
                dto,
                Request.GetClientIpAddress(),
                cancellationToken);

            SetRefreshTokenCookie(result.Tokens.RefreshToken);

            return Ok(new
            {
                message = result.Message,
                accessToken = result.Tokens.AccessToken
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize]
    [HttpPost("change-email")]
    public async Task<IActionResult> ChangeEmail(ChangeEmailDto dto, CancellationToken cancellationToken)
    {
        var userId = User.GetApplicationUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        try
        {
            var result = await _authService.ChangeEmailAsync(
                userId,
                dto,
                Request.GetClientIpAddress(),
                cancellationToken);

            SetRefreshTokenCookie(result.Tokens.RefreshToken);

            return Ok(new
            {
                message = result.Message,
                emailDeliveryEnabled = result.EmailDeliveryEnabled,
                verificationUrl = result.VerificationUrl,
                accessToken = result.Tokens.AccessToken,
                user = result.User
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (IdentityOperationException ex)
        {
            return BadRequestIdentityErrors(ex.Errors, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize]
    [HttpPost("deactivate-account")]
    public async Task<IActionResult> DeactivateAccount(
        ConfirmAccountActionDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.GetApplicationUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        try
        {
            await _authService.DeactivateAccountAsync(
                userId,
                dto,
                Request.GetClientIpAddress(),
                cancellationToken);

            DeleteRefreshTokenCookie();

            return Ok(new
            {
                message = "Your account has been deactivated and this session has been signed out."
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (CodedOperationException ex)
        {
            return CodedFailure(ex);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize]
    [HttpPost("delete-account")]
    public async Task<IActionResult> DeleteAccount(
        ConfirmAccountActionDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.GetApplicationUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        try
        {
            await _authService.DeleteAccountAsync(userId, dto, cancellationToken);
            DeleteRefreshTokenCookie();

            return Ok(new
            {
                message = "Your account has been permanently deleted."
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (CodedOperationException ex)
        {
            return CodedFailure(ex);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (refreshToken == null)
            return Unauthorized();

        var result = await _authService.RefreshTokenAsync(
            refreshToken,
            Request.GetClientIpAddress(),
            cancellationToken);

        if (result.ShouldClearCookie)
            DeleteRefreshTokenCookie();

        if (result.Status != RefreshSessionStatus.Success)
            return Unauthorized();

        SetRefreshTokenCookie(result.Tokens!.RefreshToken);
        return Ok(new { accessToken = result.Tokens.AccessToken });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.GetApplicationUserId();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await _authService.LogoutAsync(
                userId,
                Request.Cookies["refreshToken"],
                Request.GetClientIpAddress(),
                cancellationToken);
        }

        DeleteRefreshTokenCookie();
        return Ok();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetApplicationUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var profile = await _authService.GetCurrentUserProfileAsync(userId, cancellationToken);
        if (profile == null)
            return Unauthorized();

        return Ok(profile);
    }

    private void SetRefreshTokenCookie(RefreshToken token)
    {
        Response.Cookies.Append("refreshToken", token.Token, CreateRefreshTokenCookieOptions(token.Expires));
    }

    private CookieOptions CreateRefreshTokenCookieOptions(DateTime expires)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !_hostEnvironment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = expires,
            Path = "/"
        };
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = !_hostEnvironment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
    }

    private BadRequestObjectResult BadRequestIdentityErrors(
        IReadOnlyCollection<string> errors,
        string fallbackMessage)
    {
        return BadRequest(new
        {
            message = fallbackMessage,
            errors,
            code = "validation_failed"
        });
    }

    private BadRequestObjectResult BadRequestMessage(string message, string code = "bad_request")
    {
        return BadRequest(new
        {
            message,
            code
        });
    }

    private ObjectResult CodedFailure(CodedOperationException exception)
    {
        return StatusCode(exception.StatusCode, new
        {
            message = exception.Message,
            code = exception.Code
        });
    }
}
