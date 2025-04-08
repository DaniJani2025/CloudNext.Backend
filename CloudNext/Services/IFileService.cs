using CloudNext.Models;

namespace CloudNext.Services
{
    public interface IFileService
    {
        Task<UserFile> SaveEncryptedFileAsync(IFormFile file, Guid userId, Guid? folderId = null);
    }
}
