using System.Text;
using KickOffAPI.DTOs;
using KickOffAPI.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace KickOffAPI.Services
{
    public class AuthService(
        UserManager<ApplicationUser> userManager,
        AppIdentityDbContext context,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService tokenService,
        SendbirdService sendbirdService,
        UserService userService,
        IEmailService emailService,
        ClientAppUrlResolver clientAppUrlResolver,
        IWebHostEnvironment hostEnvironment,
        ILogger<AuthService> logger)
    {
        private const string DefaultUserRole = "User";
        private const string DeactivateAccountConfirmationText = "DEACTIVATE";
        private const string DeleteAccountConfirmationText = "DELETE";
        private static readonly TimeSpan DeactivatedAccountLockoutDuration = TimeSpan.FromDays(36500);

        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly AppIdentityDbContext _context = context;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly JwtTokenService _tokenService = tokenService;
        private readonly SendbirdService _sendbirdService = sendbirdService;
        private readonly UserService _userService = userService;
        private readonly IEmailService _emailService = emailService;
        private readonly ClientAppUrlResolver _clientAppUrlResolver = clientAppUrlResolver;
        private readonly IWebHostEnvironment _hostEnvironment = hostEnvironment;
        private readonly ILogger<AuthService> _logger = logger;

        public async Task<RegisterResult> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken)
        {
            var normalizedEmail = dto.Email?.Trim();
            var normalizedUserName = dto.UserName?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(normalizedUserName))
                throw new InvalidOperationException("Email and username are required.");

            var user = new ApplicationUser
            {
                UserName = normalizedUserName,
                Email = normalizedEmail
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            EnsureIdentitySuccess(createResult, "We could not create your account.");

            user = await _userManager.FindByIdAsync(user.Id)
                ?? throw new InvalidOperationException("User not found after creation, this should not happen.");

            var roleResult = await _userManager.AddToRoleAsync(user, DefaultUserRole);
            if (!roleResult.Succeeded)
            {
                await DeleteUserOrThrowAsync(
                    user,
                    "We could not finish setting up your account.",
                    roleResult.Errors);
            }

            try
            {
                await _sendbirdService.CreateUserAsync(
                    user.PublicId.ToString(),
                    user.UserName ?? normalizedUserName);
            }
            catch (Exception ex)
            {
                await DeleteUserOrThrowAsync(
                    user,
                    "Registration could not be completed because chat provisioning failed.",
                    null,
                    ex);
            }

            var confirmation = await SendEmailConfirmationAsync(user, cancellationToken);
            return new RegisterResult(confirmation);
        }

        public async Task<LoginResult> LoginAsync(AuthDto dto, string ipAddress, CancellationToken cancellationToken = default)
        {
            var user = await GetUserByCredentialsAsync(dto);
            if (user == null)
                return new LoginResult(LoginStatus.InvalidCredentials);

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, true);
            if (result.IsLockedOut)
            {
                return IsDeactivatedAccount(user)
                    ? new LoginResult(
                        LoginStatus.AccountDeactivated,
                        Message: "This account has been deactivated. Contact support to restore access.",
                        Code: "account_deactivated")
                    : new LoginResult(
                        LoginStatus.AccountLocked,
                        Message: "Account locked.",
                        Code: "account_locked");
            }

            if (result.IsNotAllowed)
            {
                return new LoginResult(
                    LoginStatus.SignInNotAllowed,
                    Message: "Sign-in is not allowed for this account.",
                    Code: "sign_in_not_allowed");
            }

            if (!result.Succeeded)
                return new LoginResult(LoginStatus.InvalidCredentials);

            var tokens = await CreateLoginSessionAsync(user.Id, ipAddress, cancellationToken);
            return new LoginResult(LoginStatus.Success, Tokens: tokens);
        }

        public async Task<ConfirmEmailResult> ConfirmEmailAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ConfirmEmailResult(
                    ConfirmEmailStatus.NotFound,
                    "We could not find an account for this confirmation link.");
            }

            if (user.EmailConfirmed)
            {
                return new ConfirmEmailResult(
                    ConfirmEmailStatus.AlreadyConfirmed,
                    "Your email is already confirmed. You can sign in.");
            }

            var decodedCode = DecodeIdentityToken(code);
            if (decodedCode == null)
            {
                return new ConfirmEmailResult(
                    ConfirmEmailStatus.InvalidLink,
                    "The email confirmation link is invalid.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, decodedCode);
            if (!result.Succeeded)
            {
                return new ConfirmEmailResult(
                    ConfirmEmailStatus.InvalidLink,
                    "That email confirmation link is invalid or has expired.");
            }

            return new ConfirmEmailResult(
                ConfirmEmailStatus.Success,
                "Email confirmed. You can sign in to KickOff now.");
        }

        public async Task<ResendConfirmationResult> ResendConfirmationAsync(
            ResendEmailConfirmationDto dto,
            CancellationToken cancellationToken)
        {
            var normalizedEmail = dto.Email?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
                throw new InvalidOperationException("Enter an email address to resend the confirmation link.");

            var user = await _userManager.FindByEmailAsync(normalizedEmail);
            if (user == null)
            {
                return new ResendConfirmationResult(
                    AlreadyConfirmed: false,
                    EmailDeliveryEnabled: _emailService.IsEnabled,
                    Message: "If an account exists for that email, we sent a fresh confirmation link.",
                    VerificationUrl: null);
            }

            if (user.EmailConfirmed)
            {
                return new ResendConfirmationResult(
                    AlreadyConfirmed: true,
                    EmailDeliveryEnabled: _emailService.IsEnabled,
                    Message: "That email is already confirmed. You can sign in.",
                    VerificationUrl: null);
            }

            var confirmation = await SendEmailConfirmationAsync(user, cancellationToken);
            return new ResendConfirmationResult(
                AlreadyConfirmed: false,
                confirmation.EmailDeliveryEnabled,
                confirmation.Message,
                confirmation.VerificationUrl);
        }

        public async Task RequestPasswordResetAsync(
            ForgotPasswordDto dto,
            CancellationToken cancellationToken)
        {
            var normalizedEmail = dto.Email?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
                throw new InvalidOperationException("Enter your email address to reset your password.");

            var user = await _userManager.FindByEmailAsync(normalizedEmail);
            if (user != null && user.EmailConfirmed)
                await SendPasswordResetEmailAsync(user, cancellationToken);
        }

        public async Task<PasswordResetResult> ResetPasswordAsync(
            ResetPasswordDto dto,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            var normalizedEmail = dto.Email?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(dto.Code))
            {
                return new PasswordResetResult(
                    false,
                    "The password reset link is incomplete.");
            }

            var decodedCode = DecodeIdentityToken(dto.Code);
            if (decodedCode == null)
            {
                return new PasswordResetResult(
                    false,
                    "That password reset link is invalid or has expired.");
            }

            var user = await _userManager.FindByEmailAsync(normalizedEmail);
            if (user == null)
            {
                return new PasswordResetResult(
                    false,
                    "That password reset link is invalid or has expired.");
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedCode, dto.NewPassword);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(error => string.Equals(error.Code, "InvalidToken", StringComparison.OrdinalIgnoreCase)))
                {
                    return new PasswordResetResult(
                        false,
                        "That password reset link is invalid or has expired.");
                }

                return new PasswordResetResult(
                    false,
                    FormatIdentityErrors(result.Errors, "We could not reset your password."));
            }

            var userWithRefreshTokens = await LoadUserWithRefreshTokensAsync(user.Id, cancellationToken)
                ?? throw new InvalidOperationException("User refresh tokens could not be loaded.");

            if (RevokeActiveRefreshTokens(userWithRefreshTokens, ipAddress))
                await _context.SaveChangesAsync(cancellationToken);

            return new PasswordResetResult(
                true,
                "Your password has been reset. Sign in with your new password.");
        }

        public async Task<ChangePasswordResult> ChangePasswordAsync(
            string userId,
            ChangePasswordDto dto,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
                throw new InvalidOperationException("Enter your current password and a new password to continue.");

            var user = await LoadUserWithRefreshTokensAsync(userId, cancellationToken);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(error => string.Equals(error.Code, "PasswordMismatch", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Your current password is incorrect.");

                throw new InvalidOperationException(
                    FormatIdentityErrors(result.Errors, "We could not update your password."));
            }

            var tokens = await RotateSessionAsync(user, ipAddress, cancellationToken);
            return new ChangePasswordResult("Your password has been updated.", tokens);
        }

        public async Task<ChangeEmailResult> ChangeEmailAsync(
            string userId,
            ChangeEmailDto dto,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            var normalizedNewEmail = dto.NewEmail?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedNewEmail) || string.IsNullOrWhiteSpace(dto.CurrentPassword))
                throw new InvalidOperationException("Enter your current password and a new email address to continue.");

            var user = await LoadUserWithRefreshTokensAsync(userId, cancellationToken);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            if (string.Equals(user.Email, normalizedNewEmail, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Enter a different email address to continue.");

            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!isCurrentPasswordValid)
                throw new InvalidOperationException("Your current password is incorrect.");

            var setEmailResult = await _userManager.SetEmailAsync(user, normalizedNewEmail);
            EnsureIdentitySuccess(setEmailResult, "We could not update your email address.");

            user.EmailConfirmed = false;

            var updateResult = await _userManager.UpdateAsync(user);
            EnsureIdentitySuccess(updateResult, "We could not update your email address.");

            var confirmation = await SendEmailConfirmationAsync(user, cancellationToken);
            var tokens = await RotateSessionAsync(user, ipAddress, cancellationToken);
            var profile = await _userService.GetUserProfile(
                user.PublicId,
                cancellationToken,
                includeAccountManagement: true)
                ?? throw new InvalidOperationException("Updated user profile could not be loaded.");

            return new ChangeEmailResult(
                $"Your email address has been updated. {confirmation.Message}",
                confirmation.EmailDeliveryEnabled,
                confirmation.VerificationUrl,
                tokens,
                profile);
        }

        public async Task DeactivateAccountAsync(
            string userId,
            ConfirmAccountActionDto dto,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            if (!MatchesConfirmationText(dto.ConfirmationText, DeactivateAccountConfirmationText))
            {
                throw new CodedOperationException(
                    $"Type {DeactivateAccountConfirmationText} to confirm account deactivation.",
                    "confirmation_required");
            }

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                throw new InvalidOperationException("Enter your current password to continue.");

            var user = await LoadUserWithRefreshTokensAsync(userId, cancellationToken);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!isCurrentPasswordValid)
                throw new InvalidOperationException("Your current password is incorrect.");

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.UtcNow.Add(DeactivatedAccountLockoutDuration);

            RevokeActiveRefreshTokens(user, ipAddress);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAccountAsync(
            string userId,
            ConfirmAccountActionDto dto,
            CancellationToken cancellationToken)
        {
            if (!MatchesConfirmationText(dto.ConfirmationText, DeleteAccountConfirmationText))
            {
                throw new CodedOperationException(
                    $"Type {DeleteAccountConfirmationText} to confirm permanent deletion.",
                    "confirmation_required");
            }

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                throw new InvalidOperationException("Enter your current password to continue.");

            var user = await LoadUserWithRefreshTokensAsync(userId, cancellationToken);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!isCurrentPasswordValid)
                throw new InvalidOperationException("Your current password is incorrect.");

            var deletionEligibility = await _userService.GetAccountDeletionEligibilityAsync(user.Id, cancellationToken);
            if (!deletionEligibility.CanDeleteAccount)
            {
                throw new CodedOperationException(
                    deletionEligibility.RestrictionMessage ?? "This account cannot be deleted right now.",
                    "delete_not_available");
            }

            await _userService.DeleteAccountAsync(user.Id, cancellationToken);
        }

        public async Task<RefreshSessionResult> RefreshTokenAsync(
            string refreshToken,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            var user = await _context.Users
                .Include(loadedUser => loadedUser.RefreshTokens)
                .SingleOrDefaultAsync(
                    loadedUser => loadedUser.RefreshTokens.Any(token => token.Token == refreshToken),
                    cancellationToken);

            if (user == null)
                return new RefreshSessionResult(RefreshSessionStatus.Unauthorized, ShouldClearCookie: true);

            if (await _userManager.IsLockedOutAsync(user))
                return new RefreshSessionResult(RefreshSessionStatus.Unauthorized, ShouldClearCookie: true);

            var token = user.RefreshTokens.SingleOrDefault(existingToken => existingToken.Token == refreshToken);
            if (token == null || !token.IsActive)
                return new RefreshSessionResult(RefreshSessionStatus.Unauthorized, ShouldClearCookie: true);

            var newRefreshToken = _tokenService.GenerateRefreshToken(ipAddress);
            token.Revoked = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReplacedByToken = newRefreshToken.Token;
            user.RefreshTokens.Add(newRefreshToken);

            await _context.SaveChangesAsync(cancellationToken);

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.CreateToken(user, roles);

            return new RefreshSessionResult(
                RefreshSessionStatus.Success,
                Tokens: new SessionTokens(accessToken, newRefreshToken));
        }

        public async Task LogoutAsync(
            string userId,
            string? refreshToken,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            var user = await LoadUserWithRefreshTokensAsync(userId, cancellationToken);
            if (user == null)
                return;

            var token = user.RefreshTokens.SingleOrDefault(existingToken => existingToken.Token == refreshToken);
            if (token == null)
                return;

            token.Revoked = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<UserProfileDto?> GetCurrentUserProfileAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return null;

            return await _userService.GetUserProfile(
                user.PublicId,
                cancellationToken,
                includeAccountManagement: true);
        }

        private async Task<ApplicationUser?> GetUserByCredentialsAsync(AuthDto dto)
        {
            ApplicationUser? user = null;

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var normalizedEmail = dto.Email.Trim();
                user = await _userManager.FindByEmailAsync(normalizedEmail)
                    ?? await _userManager.FindByNameAsync(normalizedEmail);
            }

            if (user == null && !string.IsNullOrWhiteSpace(dto.UserName))
                user = await _userManager.FindByNameAsync(dto.UserName.Trim());

            return user;
        }

        private async Task<SessionTokens> CreateLoginSessionAsync(
            string userId,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            var user = await LoadUserWithRefreshTokensAsync(userId, cancellationToken)
                ?? throw new InvalidOperationException("User not found while creating a session.");

            var refreshToken = _tokenService.GenerateRefreshToken(ipAddress);
            user.RefreshTokens.Add(refreshToken);

            await _context.SaveChangesAsync(cancellationToken);

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.CreateToken(user, roles);

            return new SessionTokens(accessToken, refreshToken);
        }

        private async Task<ApplicationUser?> LoadUserWithRefreshTokensAsync(
            string userId,
            CancellationToken cancellationToken)
        {
            return await _context.Users
                .Include(loadedUser => loadedUser.RefreshTokens)
                .SingleOrDefaultAsync(loadedUser => loadedUser.Id == userId, cancellationToken);
        }

        private async Task<SessionTokens> RotateSessionAsync(
            ApplicationUser user,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            var newRefreshToken = _tokenService.GenerateRefreshToken(ipAddress);

            RevokeActiveRefreshTokens(user, ipAddress, newRefreshToken.Token);
            user.RefreshTokens.Add(newRefreshToken);

            await _context.SaveChangesAsync(cancellationToken);

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.CreateToken(user, roles);

            return new SessionTokens(accessToken, newRefreshToken);
        }

        private async Task<EmailDispatchResult> SendEmailConfirmationAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var confirmationUrl = await BuildEmailConfirmationUrlAsync(user);
            if (string.IsNullOrWhiteSpace(confirmationUrl))
            {
                _logger.LogWarning(
                    "Unable to generate an email confirmation link for user {UserId} because no client base URL could be resolved.",
                    user.Id);

                return new EmailDispatchResult(
                    EmailDeliveryEnabled: false,
                    VerificationUrl: null,
                    Message: "Your account was created, but email verification is not configured yet. Please contact support.");
            }

            if (!_emailService.IsEnabled)
            {
                _logger.LogWarning(
                    "SMTP delivery is disabled. Returning a development-only confirmation preview for user {UserId}.",
                    user.Id);

                return new EmailDispatchResult(
                    EmailDeliveryEnabled: false,
                    VerificationUrl: _hostEnvironment.IsDevelopment() ? confirmationUrl : null,
                    Message: _hostEnvironment.IsDevelopment()
                        ? "Your account is ready. Email delivery is not configured locally, so use the preview link below to verify it."
                        : "Your account was created. We could not send a confirmation email right now, so please try again shortly.");
            }

            var emailBody =
                $"Hi {user.UserName},{Environment.NewLine}{Environment.NewLine}" +
                "Confirm your email to finish setting up your KickOff account:" +
                $"{Environment.NewLine}{Environment.NewLine}{confirmationUrl}{Environment.NewLine}{Environment.NewLine}" +
                "If you did not create this account, you can ignore this email.";

            try
            {
                await _emailService.SendAsync(
                    user.Email ?? throw new InvalidOperationException("User email is required for confirmation delivery."),
                    "Confirm your KickOff email",
                    emailBody,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send a confirmation email to user {UserId}.",
                    user.Id);

                return new EmailDispatchResult(
                    EmailDeliveryEnabled: false,
                    VerificationUrl: _hostEnvironment.IsDevelopment() ? confirmationUrl : null,
                    Message: _hostEnvironment.IsDevelopment()
                        ? "Your account is ready, but email delivery failed locally. Use the preview link below to verify it."
                        : "Your account was created, but we could not send the confirmation email yet. Please try resending it in a moment.");
            }

            return new EmailDispatchResult(
                EmailDeliveryEnabled: true,
                VerificationUrl: _hostEnvironment.IsDevelopment() ? confirmationUrl : null,
                Message: "We sent a confirmation link to your email. You can sign in now, and verify your address whenever you're ready.");
        }

        private async Task SendPasswordResetEmailAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var resetUrl = await BuildPasswordResetUrlAsync(user);
            if (string.IsNullOrWhiteSpace(resetUrl))
            {
                _logger.LogWarning(
                    "Unable to generate a password reset link for user {UserId} because no client base URL could be resolved.",
                    user.Id);

                return;
            }

            if (!_emailService.IsEnabled)
            {
                if (_hostEnvironment.IsDevelopment())
                {
                    _logger.LogWarning(
                        "SMTP delivery is disabled. Development password reset link for user {UserId}: {ResetUrl}",
                        user.Id,
                        resetUrl);
                }
                else
                {
                    _logger.LogWarning(
                        "SMTP delivery is disabled. Password reset email was skipped for user {UserId}.",
                        user.Id);
                }

                return;
            }

            var emailBody =
                $"Hi {user.UserName},{Environment.NewLine}{Environment.NewLine}" +
                "Use the link below to reset your KickOff password:" +
                $"{Environment.NewLine}{Environment.NewLine}{resetUrl}{Environment.NewLine}{Environment.NewLine}" +
                "If you did not request a password reset, you can ignore this email.";

            try
            {
                await _emailService.SendAsync(
                    user.Email ?? throw new InvalidOperationException("User email is required for password reset delivery."),
                    "Reset your KickOff password",
                    emailBody,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send a password reset email to user {UserId}.",
                    user.Id);

                if (_hostEnvironment.IsDevelopment())
                {
                    _logger.LogWarning(
                        "Development password reset link for user {UserId}: {ResetUrl}",
                        user.Id,
                        resetUrl);
                }
            }
        }

        private async Task<string?> BuildEmailConfirmationUrlAsync(ApplicationUser user)
        {
            var clientBaseUrl = _clientAppUrlResolver.Resolve();
            if (string.IsNullOrWhiteSpace(clientBaseUrl))
                return null;

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            return
                $"{clientBaseUrl}/auth/verify-email" +
                $"?userId={Uri.EscapeDataString(user.Id)}" +
                $"&code={Uri.EscapeDataString(encodedToken)}" +
                $"&email={Uri.EscapeDataString(user.Email ?? string.Empty)}";
        }

        private async Task<string?> BuildPasswordResetUrlAsync(ApplicationUser user)
        {
            var clientBaseUrl = _clientAppUrlResolver.Resolve();
            if (string.IsNullOrWhiteSpace(clientBaseUrl))
                return null;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            return
                $"{clientBaseUrl}/auth/reset-password" +
                $"?email={Uri.EscapeDataString(user.Email ?? string.Empty)}" +
                $"&code={Uri.EscapeDataString(encodedToken)}";
        }

        private static string? DecodeIdentityToken(string code)
        {
            try
            {
                return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private void EnsureIdentitySuccess(IdentityResult result, string fallbackMessage)
        {
            if (!result.Succeeded)
                throw new IdentityOperationException(fallbackMessage, result.Errors);
        }

        private async Task DeleteUserOrThrowAsync(
            ApplicationUser user,
            string fallbackMessage,
            IEnumerable<IdentityError>? errors = null,
            Exception? upstreamException = null)
        {
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                var deleteErrors = string.Join(", ", deleteResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException(
                    $"{fallbackMessage} Cleanup failed after a partial registration attempt. Rollback errors: {deleteErrors}",
                    upstreamException);
            }

            if (upstreamException != null)
                throw new UpstreamDependencyException(fallbackMessage, upstreamException.Message, upstreamException);

            throw new IdentityOperationException(
                fallbackMessage,
                errors ?? Enumerable.Empty<IdentityError>());
        }

        private static bool MatchesConfirmationText(string? providedConfirmationText, string expectedConfirmationText)
        {
            return string.Equals(
                providedConfirmationText?.Trim(),
                expectedConfirmationText,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatIdentityErrors(IEnumerable<IdentityError> errors, string fallbackMessage)
        {
            var message = string.Join(
                " ",
                errors
                    .Select(error => error.Description)
                    .Where(description => !string.IsNullOrWhiteSpace(description)));

            return string.IsNullOrWhiteSpace(message)
                ? fallbackMessage
                : message;
        }

        private static bool RevokeActiveRefreshTokens(
            ApplicationUser user,
            string ipAddress,
            string? replacementToken = null)
        {
            var revokedAny = false;
            var revokedAt = DateTime.UtcNow;

            foreach (var activeToken in user.RefreshTokens.Where(refreshToken => refreshToken.IsActive))
            {
                activeToken.Revoked = revokedAt;
                activeToken.RevokedByIp = ipAddress;
                activeToken.ReplacedByToken = replacementToken;
                revokedAny = true;
            }

            return revokedAny;
        }

        private static bool IsDeactivatedAccount(ApplicationUser user)
        {
            var lockoutEnd = user.LockoutEnd;
            return user.LockoutEnabled
                && lockoutEnd.HasValue
                && lockoutEnd.Value > DateTimeOffset.UtcNow.AddYears(50);
        }
    }
}
