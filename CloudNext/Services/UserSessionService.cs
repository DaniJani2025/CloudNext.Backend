using System.Collections.Concurrent;
using System.Security.Claims;
using CloudNext.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CloudNext.Services
{
    public class UserSessionService : IUserSessionService
    {
        private static readonly ConcurrentDictionary<Guid, (string Key, string IP, string Agent)> _userSessions = new();
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserSessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void SetSession(Guid userId, string key)
        {
            var context = _httpContextAccessor.HttpContext;
            var ipAddress = context?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context?.Request.Headers.UserAgent.ToString() ?? "unknown";

            _userSessions[userId] = (key, ipAddress, userAgent);
        }

        public string? GetEncryptionKey(Guid userId)
        {
            var context = _httpContextAccessor.HttpContext;
            var currentIp = context?.Connection.RemoteIpAddress?.ToString();
            var currentUserAgent = context?.Request.Headers.UserAgent.ToString();

            Console.WriteLine($"Current session: {currentIp}, {currentUserAgent}");

            if (currentIp == null || currentUserAgent == null)
                return null;

            if (_userSessions.TryGetValue(userId, out var session))
            {
                Console.WriteLine($"User Session: {session.IP}, {session.Agent}");
                if (session.IP == currentIp && session.Agent == currentUserAgent)
                    return session.Key;
            }

            return null;
        }

        public void RemoveSession(Guid userId)
        {
            _userSessions.TryRemove(userId, out _);
        }
    }
}
