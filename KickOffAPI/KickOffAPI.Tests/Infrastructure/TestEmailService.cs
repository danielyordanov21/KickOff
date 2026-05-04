namespace KickOffAPI.Tests.Infrastructure;

public sealed record CapturedEmail(string ToAddress, string Subject, string Body);

public sealed class TestEmailService : IEmailService
{
    public bool IsEnabled { get; set; } = true;

    public Exception? SendException { get; set; }

    public List<CapturedEmail> SentEmails { get; } = [];

    public Task SendAsync(
        string toAddress,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (SendException != null)
            throw SendException;

        SentEmails.Add(new CapturedEmail(toAddress, subject, body));
        return Task.CompletedTask;
    }

    public void Clear()
    {
        IsEnabled = true;
        SendException = null;
        SentEmails.Clear();
    }
}
