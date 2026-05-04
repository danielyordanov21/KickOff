public interface IEmailService
{
    bool IsEnabled { get; }

    Task SendAsync(
        string toAddress,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
