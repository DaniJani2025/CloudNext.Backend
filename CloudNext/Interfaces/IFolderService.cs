using CloudNext.DTOs.UserFolder;

namespace CloudNext.Interfaces
{
    public interface IFolderService
    {
        Task<FolderResponseDto> CreateFolderAsync(CreateFolderDto dto);
        Task<byte[]> DownloadFolderAsync(Guid userId, Guid folderId);
        Task UploadFolderAsync(Guid userId, FolderUploadDto dto);
        Task<List<FolderResponseDto>> GetFoldersInCurrentDirectoryAsync(Guid userId, Guid? folderId);
        Task<FolderTreeDto> GetFullFolderStructureAsync(Guid userId);
    }
}
