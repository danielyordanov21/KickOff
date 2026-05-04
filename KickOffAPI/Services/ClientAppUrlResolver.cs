using KickOffAPI.Models;
using Microsoft.Extensions.Options;

public class ClientAppUrlResolver(
    IOptionsMonitor<AuthOptions> authOptionsMonitor,
    IOptionsMonitor<ProjectNotificationOptions> projectNotificationOptionsMonitor,
    IWebHostEnvironment hostEnvironment)
{
    private const string DevelopmentClientBaseUrl = "http://localhost:4200";

    private readonly IOptionsMonitor<AuthOptions> _authOptionsMonitor = authOptionsMonitor;
    private readonly IOptionsMonitor<ProjectNotificationOptions> _projectNotificationOptionsMonitor = projectNotificationOptionsMonitor;
    private readonly IWebHostEnvironment _hostEnvironment = hostEnvironment;

    public string? Resolve()
    {
        var configuredBaseUrl = Normalize(_authOptionsMonitor.CurrentValue.ClientBaseUrl);
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            return configuredBaseUrl;

        configuredBaseUrl = Normalize(_projectNotificationOptionsMonitor.CurrentValue.ClientBaseUrl);
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            return configuredBaseUrl;

        return _hostEnvironment.IsDevelopment()
            ? DevelopmentClientBaseUrl
            : null;
    }

    private static string? Normalize(string? value)
    {
        var trimmedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmedValue)
            ? null
            : trimmedValue.TrimEnd('/');
    }
}
