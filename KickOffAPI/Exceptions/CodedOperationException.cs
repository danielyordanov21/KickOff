using Microsoft.AspNetCore.Http;

namespace KickOffAPI.Exceptions
{
    public sealed class CodedOperationException(
        string message,
        string code,
        int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
    {
        public string Code { get; } = code;

        public int StatusCode { get; } = statusCode;
    }
}
