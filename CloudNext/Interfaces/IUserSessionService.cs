namespace CloudNext.Interfaces
{
    public interface IUserSessionService
    {
        void SetEncryptionKey(Guid userId, string key);
        string? GetEncryptionKey(Guid userId);
        void RemoveEncryptionKey(Guid userId);
    }
}
