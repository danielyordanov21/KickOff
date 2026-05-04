using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace KickOffAPI.Tests.Infrastructure;

public sealed class TestWebHostEnvironment(string environmentName = "Production") : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "KickOffAPI.Tests";

    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

    public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();

    public string EnvironmentName { get; set; } = environmentName;

    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
