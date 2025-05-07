namespace CloudNext.Interfaces
{
    public interface IUserSessionService
    {
        Task SetSession(Guid userId, string key);
        Task<string?> GetEncryptionKey(Guid userId);
        Task RemoveSession(Guid userId);
    }
}
