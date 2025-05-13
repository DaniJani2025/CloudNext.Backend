using CloudNext.Models;

namespace CloudNext.Interfaces
{
    public interface IUserFolderRepository
    {
        Task<UserFolder?> GetFolderAsync(Guid userId, Guid? parentFolderId, string folderName);
        Task AddFolderAsync(UserFolder folder);
        Task<UserFolder?> GetFolderByIdAsync(Guid folderId);
        Task<UserFolder?> GetRootFolderAsync(Guid userId);
        Task<List<UserFolder>> GetFoldersByParentIdAsync(Guid userId, Guid parentId);
    }
}
