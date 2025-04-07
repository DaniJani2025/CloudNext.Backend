using System.Collections.Concurrent;
using CloudNext.Interfaces;

namespace CloudNext.Services
{
    public class UserSessionService : IUserSessionService
    {
        private static readonly ConcurrentDictionary<Guid, string> _userKeys = new();

        public void SetEncryptionKey(Guid userId, string key)
        {
            _userKeys[userId] = key;
        }

        public string? GetEncryptionKey(Guid userId)
        {
            _userKeys.TryGetValue(userId, out var key);
            return key;
        }

        public void RemoveEncryptionKey(Guid userId)
        {
            _userKeys.TryRemove(userId, out _);
        }
    }
}