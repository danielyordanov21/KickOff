namespace KickOffAPI.Extensions
{
    public static class HttpRequestExtensions
    {
        public static string GetClientIpAddress(this HttpRequest request)
        {
            if (request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
                return forwardedFor.ToString();

            return request.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
        }
    }
}
