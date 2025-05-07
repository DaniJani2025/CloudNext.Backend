using CloudNext.DTOs.UserFolder;
using CloudNext.Interfaces;
using CloudNext.Models;
using CloudNext.Utils;
using CloudNext.Common;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.IO.Compression;
using static System.Net.Mime.MediaTypeNames;

namespace CloudNext.Services
{
    public class FolderService : IFolderService
    {
        private readonly IUserFolderRepository _userFolderRepository;
        private readonly IUserFileRepository _fileRepository;
        private readonly IFileService _fileService;
        private readonly IUserSessionService _userSessionService;

        public FolderService(
            IUserFolderRepository userFolderRepository, 
            IFileService fileService, 
            IUserFileRepository fileRepository,
            IUserSessionService userSessionService
            )
        {
            _userFolderRepository = userFolderRepository;
            _fileService = fileService;
            _fileRepository = fileRepository;
            _userSessionService = userSessionService;
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
            var rootFolder = await _userFolderRepository.GetFolderByIdAsync(folderId);
            if (rootFolder == null || rootFolder.UserId != userId)
                throw new InvalidOperationException("Folder not found or access denied.");

            var userKey = _userSessionService.GetEncryptionKey(userId);
            if (string.IsNullOrEmpty(userKey))
                throw new InvalidOperationException("Encryption key not found for the user.");

            var allFilesWithPaths = await CollectFilesRecursively(userId, rootFolder);

            using var zipMemoryStream = new MemoryStream();
            using (var archive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var (file, relativePath) in allFilesWithPaths)
                {
                    var fileSystemPath = Path.Combine(AppContext.BaseDirectory, file.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (!File.Exists(fileSystemPath)) continue;

                    var encryptedBytes = await File.ReadAllBytesAsync(fileSystemPath);
                    var decryptedBytes = EncryptionHelper.DecryptFileBytes(encryptedBytes, userKey);

                    var zipEntry = archive.CreateEntry(relativePath, CompressionLevel.Fastest);
                    using var entryStream = zipEntry.Open();
                    await entryStream.WriteAsync(decryptedBytes, 0, decryptedBytes.Length);
                }
            }

            zipMemoryStream.Position = 0;
            return zipMemoryStream.ToArray();
        }

        public async Task<UploadResultDto> UploadFolderAsync(Guid userId, FolderUploadDto dto)
        {
            var uploaded = 0;
            var skipped = 0;

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

                string detectedContentType = MimeHelper.GetMimeType(entry.Name, ms.ToArray());
                var ext = Path.GetExtension(entry.Name)?.ToLowerInvariant();

                if (!Constants.Media.SupportedImageTypes.Contains(detectedContentType)
                    && !Constants.Media.SupportedVideoTypes.Contains(detectedContentType)
                    && !Constants.Media.CommonFileLogos.ContainsKey(ext!))
                    {
                        skipped++;
                        continue;
                    }

                ms.Position = 0;

                var formFile = new FormFile(ms, 0, ms.Length, null, entry.Name)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = detectedContentType
                };

                await _fileService.SaveEncryptedFileAsync(formFile, currentParentId, userId);
                uploaded++;
            }

            return new UploadResultDto { UploadedCount = uploaded, SkippedCount = skipped };
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

        private async Task<List<(UserFile file, string relativePath)>> CollectFilesRecursively(Guid userId, UserFolder folder)
        {
            var result = new List<(UserFile, string)>();

            var files = await _fileRepository.GetFilesByFolderIdAsync(folder.Id);
            foreach (var file in files)
            {
                var relativePath = Path.Combine(folder.VirtualPath.TrimStart('/'), file.OriginalName).Replace("\\", "/");
                result.Add((file, relativePath));
            }

            var subfolders = await _userFolderRepository.GetFoldersByParentIdAsync(userId, folder.Id);
            foreach (var subfolder in subfolders)
            {
                result.AddRange(await CollectFilesRecursively(userId, subfolder));
            }

            return result;
        }

        public static class MimeHelper
        {
            private static readonly Dictionary<string, string> MimeTypes = new()
            {
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".png", "image/png" },
                { ".gif", "image/gif" },
                { ".bmp", "image/bmp" },
                { ".mp4", "video/mp4" },
                { ".mov", "video/quicktime" },
                { ".avi", "video/x-msvideo" },
                { ".webm", "video/webm" },
            };

            public static string GetMimeType(string fileName, byte[] fileBytes)
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (MimeTypes.TryGetValue(extension, out var mime))
                    return mime;

                return "application/octet-stream";
            }
        }
    }
}
