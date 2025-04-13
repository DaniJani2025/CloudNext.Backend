using CloudNext.DTOs.UserFiles;
using CloudNext.Models;

namespace CloudNext.Interfaces
{
    public interface IFileService
    {
        Task<(byte[] Data, string FileName, string ContentType)> GetDecryptedFilesAsync(List<Guid> fileIds, Guid userId);
        Task<UserFile> SaveEncryptedFileAsync(IFormFile file, Guid parentFolderId);
    }
}
