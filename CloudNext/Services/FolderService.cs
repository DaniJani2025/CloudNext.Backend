using CloudNext.DTOs.UserFolder;
using CloudNext.Interfaces;
using CloudNext.Models;
using CloudNext.Repositories.Users;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.IO.Compression;

namespace CloudNext.Services
{
    public class FolderService
    {
        private readonly IUserFolderRepository _userFolderRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IFileService _fileService;

        public FolderService(IUserFolderRepository userFolderRepository, IFileService fileService, IFileRepository fileRepository)
        {
            _userFolderRepository = userFolderRepository;
            _fileService = fileService;
            _fileRepository = fileRepository;
        }

        public async Task<FolderResponseDto> CreateFolderAsync(CreateFolderDto dto)
        {
            Guid? parentFolderId = dto.ParentFolderId;

            if (!parentFolderId.HasValue)
            {
                var rootFolder = await _userFolderRepository.GetRootFolderAsync(dto.UserId);
                if (rootFolder == null)
                    throw new InvalidOperationException("Root folder not found for user.");

                parentFolderId = rootFolder.Id;
            }

            var existingFolder = await _userFolderRepository.GetFolderAsync(dto.UserId, parentFolderId, dto.FolderName);
            if (existingFolder != null)
                throw new InvalidOperationException("A folder with the same name already exists in this location.");

            var parent = await _userFolderRepository.GetFolderByIdAsync(parentFolderId.Value);
            if (parent == null)
                throw new InvalidOperationException("Parent folder not found.");

            string virtualPath = Path.Combine(parent.VirtualPath, dto.FolderName).Replace("\\", "/");

            var newFolder = new UserFolder
            {
                Name = dto.FolderName,
                UserId = dto.UserId,
                ParentFolderId = parent.Id,
                VirtualPath = virtualPath
            };

            await _userFolderRepository.AddFolderAsync(newFolder);

            return new FolderResponseDto
            {
                FolderId = newFolder.Id,
                Name = newFolder.Name,
                VirtualPath = newFolder.VirtualPath,
                ParentFolderId = newFolder.ParentFolderId
            };
        }

        public async Task<byte[]> DownloadFolderAsync(Guid userId, Guid folderId)
        {
            var folder = await _userFolderRepository.GetFolderByIdAsync(folderId);
            if (folder == null || folder.UserId != userId)
                throw new InvalidOperationException("Folder not found or access denied.");

            var filesInFolder = await _fileRepository.GetFilesByFolderIdAsync(folderId);

            using var zipMemoryStream = new MemoryStream();
            using (var archive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var file in filesInFolder)
                {
                    var (decryptedBytes, originalName, _) = await _fileService.GetDecryptedFilesAsync(new List<Guid> { file.Id }, userId);

                    var entryPath = Path.Combine(folder.Name, originalName).Replace("\\", "/");
                    var zipEntry = archive.CreateEntry(entryPath, CompressionLevel.Fastest);
                    using var entryStream = zipEntry.Open();
                    await entryStream.WriteAsync(decryptedBytes, 0, decryptedBytes.Length);
                }
            }

            zipMemoryStream.Position = 0;
            return zipMemoryStream.ToArray();
        }

    }
}
