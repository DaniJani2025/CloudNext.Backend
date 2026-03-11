namespace CloudNext.Interfaces
{
    public interface IStorageService
    {
        Task SaveAsync(Stream stream, string key);
        Task<Stream> GetAsync(string key);
        Task DeleteAsync(string key);
    }
}
