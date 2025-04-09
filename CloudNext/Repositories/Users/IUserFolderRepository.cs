using CloudNext.Models;

namespace CloudNext.Repositories.Users
{
    public interface IUserFolderRepository
    {
        Task<UserFolder?> GetFolderAsync(Guid userId, Guid? parentFolderId, string folderName);
        Task AddFolderAsync(UserFolder folder);
        Task<UserFolder?> GetFolderByIdAsync(Guid folderId);
    }
}
