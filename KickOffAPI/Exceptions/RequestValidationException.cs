namespace KickOffAPI.Exceptions
{
    public sealed class RequestValidationException : Exception
    {
        public RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
            : base("One or more validation errors occurred.")
        {
            Errors = errors.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.ToArray(),
                StringComparer.Ordinal);
        }

        public IReadOnlyDictionary<string, string[]> Errors { get; }
    }
}
