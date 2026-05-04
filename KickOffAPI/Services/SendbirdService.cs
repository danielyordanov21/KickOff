using KickOffAPI.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;

namespace KickOffAPI.Services
{
    public class SendbirdService
    {
        private readonly HttpClient _http;
        private readonly SendbirdModel _options;

        public SendbirdService(HttpClient http, IOptions<SendbirdModel> options)
        {
            _http = http;
            _options = options.Value;

            if (string.IsNullOrWhiteSpace(_options.AppId))
                throw new InvalidOperationException(
                    "Sendbird:AppId is not configured. Provide it via environment variables or dotnet user-secrets.");

            if (string.IsNullOrWhiteSpace(_options.ApiToken))
                throw new InvalidOperationException(
                    "Sendbird:ApiToken is not configured. Provide it via environment variables or dotnet user-secrets.");

            _http.BaseAddress =
                new Uri($"https://api-{_options.AppId}.sendbird.com/v3/");

            _http.DefaultRequestHeaders.Remove("Api-Token");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Api-Token", _options.ApiToken);
        }

        public async Task CreateUserAsync(string publicId, string nickname, string? profileUrl = null)
        {
            var body = new
            {
                user_id = publicId,
                nickname = nickname,
                profile_url = profileUrl ?? string.Empty
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var response = await _http.PostAsync("users", content);
            await EnsureSuccessAsync(response, "user creation");
        }

        public async Task EnsureUserAsync(string publicId, string nickname, string? profileUrl = null)
        {
            var response = await _http.GetAsync($"users/{publicId}");

            if (response.IsSuccessStatusCode)
                return;

            var error = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.NotFound && !IsMissingUserError(error))
                throw new Exception($"Sendbird user lookup failed: {error}");

            var body = new
            {
                user_id = publicId,
                nickname = nickname,
                profile_url = profileUrl ?? string.Empty
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var createResponse = await _http.PostAsync("users", content);

            if (createResponse.IsSuccessStatusCode)
                return;

            var createError = await createResponse.Content.ReadAsStringAsync();

            if (IsAlreadyExistsError(createError))
                return;

            throw new Exception($"Sendbird user creation failed: {createError}");
        }

        public async Task UpdateUserAsync(string publicId, string nickname, string? profileUrl = null)
        {
            var body = new
            {
                nickname = nickname,
                profile_url = profileUrl ?? string.Empty
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var response = await _http.PutAsync($"users/{publicId}", content);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                await CreateUserAsync(publicId, nickname, profileUrl);
                return;
            }

            await EnsureSuccessAsync(response, "user update");
        }

        public async Task<string> CreateSessionToken(string userId)
        {
            var body = new { user_id = userId };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var response = await _http.PostAsync($"users/{userId}/token", content);
            await EnsureSuccessAsync(response, "session token creation");

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("token").GetString()
                ?? throw new Exception();
        }

        public async Task<string> CreateChannelAsync(string user1, string user2)
        {
            var userIds = new[] { user1, user2 }
                .Where(userId => !string.IsNullOrWhiteSpace(userId))
                .Select(userId => userId.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (userIds.Length != 2)
                throw new ArgumentException("Distinct direct channels require two unique user IDs.");

            var body = new
            {
                user_ids = userIds,
                is_distinct = true,
                strict = true
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var response = await _http.PostAsync("group_channels", content);
            await EnsureSuccessAsync(response, "channel creation");

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("channel_url").GetString()
                ?? throw new Exception();
        }

        public async Task<string> GetUserChannels(string userId)
        {
            var response = await _http.GetAsync(
                $"users/{userId}/my_group_channels");
            await EnsureSuccessAsync(response, "channel lookup");

            return await response.Content.ReadAsStringAsync();
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
        {
            if (response.IsSuccessStatusCode)
                return;

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Sendbird {operation} failed: {error}");
        }

        private static bool IsMissingUserError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return false;

            try
            {
                using var doc = JsonDocument.Parse(error);
                return doc.RootElement.TryGetProperty("code", out var codeElement)
                    && codeElement.TryGetInt32(out var code)
                    && (code == 400201 || code == 400301);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool IsAlreadyExistsError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return false;

            try
            {
                using var doc = JsonDocument.Parse(error);
                return doc.RootElement.TryGetProperty("code", out var codeElement)
                    && codeElement.TryGetInt32(out var code)
                    && code == 400202;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
