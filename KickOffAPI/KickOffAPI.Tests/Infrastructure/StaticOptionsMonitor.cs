using Microsoft.Extensions.Options;

namespace KickOffAPI.Tests.Infrastructure;

public sealed class StaticOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    where T : class
{
    public T CurrentValue { get; } = currentValue;

    public T Get(string? name) => CurrentValue;

    public IDisposable OnChange(Action<T, string?> listener) => NoOpDisposable.Instance;

    private sealed class NoOpDisposable : IDisposable
    {
        public static NoOpDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
