using CloudNext.DTOs.UserFiles;
using CloudNext.Models;

namespace CloudNext.Repositories.Users
{
    public interface IFileRepository
    {
        Task AddFileAsync(UserFile file);
        Task<List<UserFile>> GetFilesByIdsAsync(List<Guid> fileIds);
        Task<List<UserFile>> GetFilesByFolderIdAsync(Guid folderId);
    }
}
