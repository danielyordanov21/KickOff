namespace KickOffAPI.Exceptions
{
    public sealed class UpstreamDependencyException(
        string message,
        string? detail = null,
        Exception? innerException = null) : Exception(message, innerException)
    {
        public string? Detail { get; } = detail;
    }
}
