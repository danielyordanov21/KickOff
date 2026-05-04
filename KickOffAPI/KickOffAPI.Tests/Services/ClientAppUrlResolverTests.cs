using KickOffAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;

namespace KickOffAPI.Tests.Services;

public class ClientAppUrlResolverTests
{
    [Fact]
    public void Resolve_ReturnsAuthClientBaseUrl_WhenConfigured()
    {
        var resolver = TestServiceFactory.CreateClientAppUrlResolver(
            authClientBaseUrl: "https://app.example.test/");

        var result = resolver.Resolve();

        Assert.Equal("https://app.example.test", result);
    }

    [Fact]
    public void Resolve_FallsBackToProjectNotificationClientBaseUrl_WhenAuthBaseUrlIsMissing()
    {
        var resolver = TestServiceFactory.CreateClientAppUrlResolver(
            authClientBaseUrl: null,
            projectNotificationClientBaseUrl: "https://notifications.example.test/");

        var result = resolver.Resolve();

        Assert.Equal("https://notifications.example.test", result);
    }

    [Fact]
    public void Resolve_ReturnsDevelopmentFallback_WhenNoUrlsAreConfigured()
    {
        var resolver = TestServiceFactory.CreateClientAppUrlResolver(
            environmentName: "Development");

        var result = resolver.Resolve();

        Assert.Equal("http://localhost:4200", result);
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenNoUrlsAreConfiguredOutsideDevelopment()
    {
        var resolver = TestServiceFactory.CreateClientAppUrlResolver(
            environmentName: "Production");

        var result = resolver.Resolve();

        Assert.Null(result);
    }
}
