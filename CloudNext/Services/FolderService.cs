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

        public async Task UploadFolderAsync(Guid userId, FolderUploadDto dto)
        {
            var parentFolder = await _userFolderRepository.GetFolderByIdAsync(Guid.Parse(dto.ParentFolderId));
            if (parentFolder == null || parentFolder.UserId != userId)
                throw new InvalidOperationException("Parent folder not found or access denied.");

            using var zipStream = dto.ZipFile.OpenReadStream();
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;

                var relativePath = Path.GetDirectoryName(entry.FullName)?.Replace("\\", "/") ?? "";
                var folderNames = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                Guid currentParentId = parentFolder.Id;
                string currentVirtualPath = parentFolder.VirtualPath;

                foreach (var folderName in folderNames)
                {
                    var existing = await _userFolderRepository.GetFolderAsync(userId, currentParentId, folderName);
                    if (existing != null)
                    {
                        currentParentId = existing.Id;
                        currentVirtualPath = existing.VirtualPath;
                    }
                    else
                    {
                        var newFolder = new UserFolder
                        {
                            UserId = userId,
                            ParentFolderId = currentParentId,
                            Name = folderName,
                            VirtualPath = Path.Combine(currentVirtualPath, folderName).Replace("\\", "/")
                        };
                        await _userFolderRepository.AddFolderAsync(newFolder);

                        currentParentId = newFolder.Id;
                        currentVirtualPath = newFolder.VirtualPath;
                    }
                }

                using var entryStream = entry.Open();
                using var ms = new MemoryStream();
                await entryStream.CopyToAsync(ms);
                ms.Position = 0;

                var formFile = new FormFile(ms, 0, ms.Length, null, entry.Name)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/octet-stream"
                };

                await _fileService.SaveEncryptedFileAsync(formFile, currentParentId, userId);
            }
        }

        public async Task<List<FolderResponseDto>> GetFoldersInCurrentDirectoryAsync(Guid userId, Guid? folderId)
        {
            Guid parentId;

            if (folderId.HasValue)
            {
                parentId = folderId.Value;
            }
            else
            {
                var rootFolder = await _userFolderRepository.GetRootFolderAsync(userId)
                                 ?? throw new InvalidOperationException("Root folder not found.");
                parentId = rootFolder.Id;
            }

            var subFolders = await _userFolderRepository.GetFoldersByParentIdAsync(userId, parentId);

            return subFolders.Select(f => new FolderResponseDto
            {
                FolderId = f.Id,
                Name = f.Name,
                VirtualPath = f.VirtualPath,
                ParentFolderId = f.ParentFolderId
            }).ToList();
        }

        public async Task<FolderTreeDto> GetFullFolderStructureAsync(Guid userId)
        {
            var rootFolder = await _userFolderRepository.GetRootFolderAsync(userId);
            if (rootFolder == null)
                throw new InvalidOperationException("Root folder not found.");

            return await BuildFolderTreeAsync(rootFolder);
        }

        private async Task<FolderTreeDto> BuildFolderTreeAsync(UserFolder folder)
        {
            var subFolders = await _userFolderRepository.GetFoldersByParentIdAsync(folder.UserId, folder.Id);

            var subFolderTrees = new List<FolderTreeDto>();
            foreach (var subFolder in subFolders)
            {
                var subTree = await BuildFolderTreeAsync(subFolder);
                subFolderTrees.Add(subTree);
            }

            return new FolderTreeDto
            {
                FolderId = folder.Id,
                Name = folder.Name,
                VirtualPath = folder.VirtualPath,
                SubFolders = subFolderTrees
            };
        }
    }
}
