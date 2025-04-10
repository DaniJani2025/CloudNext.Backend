using CloudNext.Models;

namespace CloudNext.Repositories.Users
{
    public interface IFileRepository
    {
        Task AddFileAsync(UserFile file);
    }
}
