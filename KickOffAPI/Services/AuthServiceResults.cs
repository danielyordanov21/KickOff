using KickOffAPI.DTOs;

namespace KickOffAPI.Services
{
    public sealed record EmailDispatchResult(
        bool EmailDeliveryEnabled,
        string? VerificationUrl,
        string Message);

    public sealed record RegisterResult(EmailDispatchResult Confirmation);

    public sealed record SessionTokens(
        string AccessToken,
        RefreshToken RefreshToken);

    public enum LoginStatus
    {
        Success,
        InvalidCredentials,
        AccountLocked,
        AccountDeactivated,
        SignInNotAllowed
    }

    public sealed record LoginResult(
        LoginStatus Status,
        SessionTokens? Tokens = null,
        string? Message = null,
        string? Code = null);

    public enum ConfirmEmailStatus
    {
        Success,
        AlreadyConfirmed,
        InvalidLink,
        NotFound
    }

    public sealed record ConfirmEmailResult(
        ConfirmEmailStatus Status,
        string Message);

    public sealed record ResendConfirmationResult(
        bool AlreadyConfirmed,
        bool EmailDeliveryEnabled,
        string Message,
        string? VerificationUrl);

    public sealed record PasswordResetResult(
        bool Succeeded,
        string Message);

    public sealed record ChangePasswordResult(
        string Message,
        SessionTokens Tokens);

    public sealed record ChangeEmailResult(
        string Message,
        bool EmailDeliveryEnabled,
        string? VerificationUrl,
        SessionTokens Tokens,
        UserProfileDto User);

    public enum RefreshSessionStatus
    {
        Success,
        Unauthorized
    }

    public sealed record RefreshSessionResult(
        RefreshSessionStatus Status,
        SessionTokens? Tokens = null,
        bool ShouldClearCookie = false);
}
