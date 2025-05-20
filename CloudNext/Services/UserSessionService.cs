using System.Text.Json;
using CloudNext.Interfaces;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;

namespace CloudNext.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDatabase _redis;

        public UserSessionService(IHttpContextAccessor httpContextAccessor, IConnectionMultiplexer redis)
        {
            _httpContextAccessor = httpContextAccessor;
            _redis = redis.GetDatabase();
        }

        public async Task SetSession(Guid userId, string key)
        {
            var context = _httpContextAccessor.HttpContext;
            var ipAddress = context?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context?.Request.Headers.UserAgent.ToString() ?? "unknown";

            var session = new UserSession { Key = key, IP = ipAddress, Agent = userAgent };
            var json = JsonSerializer.Serialize(session);
            await _redis.StringSetAsync(GetRedisKey(userId), json);
        }

        public async Task<string?> GetEncryptionKey(Guid userId)
        {
            var context = _httpContextAccessor.HttpContext;
            var currentIp = context?.Connection.RemoteIpAddress?.ToString();
            var currentUserAgent = context?.Request.Headers.UserAgent.ToString();

            if (currentIp == null || currentUserAgent == null)
                return null;

            var json = await _redis.StringGetAsync(GetRedisKey(userId));
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var session = JsonSerializer.Deserialize<UserSession>(json!);

            if (session != null && session.IP == currentIp && session.Agent == currentUserAgent)
                return session.Key;

            return null;
        }

        public async Task RemoveSession(Guid userId)
        {
            await _redis.KeyDeleteAsync(GetRedisKey(userId));
        }

        private static string GetRedisKey(Guid userId) => $"userSession:{userId}";

        private class UserSession
        {
            public string Key { get; set; } = default!;
            public string IP { get; set; } = default!;
            public string Agent { get; set; } = default!;
        }
    }
}
