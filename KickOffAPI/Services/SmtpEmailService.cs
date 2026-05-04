using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

public class SmtpEmailService(IOptionsMonitor<SmtpOptions> optionsMonitor, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly IOptionsMonitor<SmtpOptions> _optionsMonitor = optionsMonitor;
    private readonly ILogger<SmtpEmailService> _logger = logger;

    public bool IsEnabled =>
        _optionsMonitor.CurrentValue.Enabled &&
        !string.IsNullOrWhiteSpace(_optionsMonitor.CurrentValue.Host) &&
        !string.IsNullOrWhiteSpace(_optionsMonitor.CurrentValue.FromAddress);

    public async Task SendAsync(
        string toAddress,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug(
                "Skipping SMTP email to {ToAddress} because SMTP delivery is not configured.",
                toAddress);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var options = _optionsMonitor.CurrentValue;

        using var message = new MailMessage
        {
            From = string.IsNullOrWhiteSpace(options.FromName)
                ? new MailAddress(options.FromAddress)
                : new MailAddress(options.FromAddress, options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        message.To.Add(toAddress);

        using var client = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = string.IsNullOrWhiteSpace(options.UserName)
        };

        if (!string.IsNullOrWhiteSpace(options.UserName))
        {
            client.Credentials = new NetworkCredential(
                options.UserName,
                options.Password);
        }

        await client.SendMailAsync(message);
    }
}
