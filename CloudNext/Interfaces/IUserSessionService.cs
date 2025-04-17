namespace CloudNext.Interfaces
{
    public interface IUserSessionService
    {
        void SetSession(Guid userId, string key);
        string? GetEncryptionKey(Guid userId);
        void RemoveSession(Guid userId);
    }
}
