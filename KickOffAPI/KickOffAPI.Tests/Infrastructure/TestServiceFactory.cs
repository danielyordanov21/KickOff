using KickOffAPI.Models;
using KickOffAPI.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KickOffAPI.Tests.Infrastructure;

public static class TestServiceFactory
{
    public const string BlobConnectionString = "UseDevelopmentStorage=true";

    public static IConfiguration CreateConfiguration(IDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "kickoff-tests-signing-key-1234567890",
            ["Jwt:Issuer"] = "KickOff.Tests",
            ["Jwt:Audience"] = "KickOff.Tests.Client",
            ["Jwt:ExpiresMinutes"] = "60",
            ["AzureBlob:ConnectionString"] = BlobConnectionString,
            ["AzureBlob:ContainerName"] = "kickoff-tests",
            ["Sendbird:AppId"] = "test-app",
            ["Sendbird:ApiToken"] = "test-token",
            ["Auth:ClientBaseUrl"] = "https://client.test",
            ["ProjectNotifications:ClientBaseUrl"] = "https://client.test"
        };

        if (overrides != null)
        {
            foreach (var (key, value) in overrides)
                values[key] = value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    public static BlobService CreateBlobService(IConfiguration? configuration = null)
    {
        return new BlobService(configuration ?? CreateConfiguration());
    }

    public static SendbirdService CreateSendbirdService(HttpMessageHandler? handler = null)
    {
        var client = handler == null
            ? new HttpClient()
            : new HttpClient(handler);

        return new SendbirdService(
            client,
            Options.Create(new SendbirdModel
            {
                AppId = "test-app",
                ApiToken = "test-token"
            }));
    }

    public static ClientAppUrlResolver CreateClientAppUrlResolver(
        string? authClientBaseUrl = null,
        string? projectNotificationClientBaseUrl = null,
        string environmentName = "Production")
    {
        return new ClientAppUrlResolver(
            new StaticOptionsMonitor<AuthOptions>(new AuthOptions
            {
                ClientBaseUrl = authClientBaseUrl
            }),
            new StaticOptionsMonitor<ProjectNotificationOptions>(new ProjectNotificationOptions
            {
                ClientBaseUrl = projectNotificationClientBaseUrl
            }),
            new TestWebHostEnvironment(environmentName));
    }

    public static ILogger<T> CreateLogger<T>() => NullLogger<T>.Instance;
}
