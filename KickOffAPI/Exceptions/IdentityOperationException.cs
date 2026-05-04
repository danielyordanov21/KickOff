using Microsoft.AspNetCore.Identity;

namespace KickOffAPI.Exceptions
{
    public sealed class IdentityOperationException : Exception
    {
        public IdentityOperationException(string fallbackMessage, IEnumerable<IdentityError> errors)
            : base(fallbackMessage)
        {
            Errors = errors
                .Select(error => error.Description?.Trim())
                .Where(description => !string.IsNullOrWhiteSpace(description))
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        public IReadOnlyCollection<string> Errors { get; }
    }
}
