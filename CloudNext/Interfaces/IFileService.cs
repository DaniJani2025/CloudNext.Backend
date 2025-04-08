using CloudNext.Models;

namespace CloudNext.Interfaces
{
    public interface IFileService
    {
        Task<UserFile> SaveEncryptedFileAsync(IFormFile file, Guid userId, Guid? folderId = null);
    }
}
