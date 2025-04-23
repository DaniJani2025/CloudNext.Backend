using System.IO.Compression;
using CloudNext.Data;
using CloudNext.DTOs.UserFiles;
using CloudNext.Interfaces;
using CloudNext.Models;
using CloudNext.Repositories.Users;
using CloudNext.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloudNext.Services
{
    public class FileService : IFileService
    {
        private readonly IUserSessionService _userSessionService;
        private readonly IFileRepository _fileRepository;
        private readonly IUserFolderRepository _userFolderRepository;

        public FileService(IUserSessionService userSessionService, IFileRepository fileRepository, IUserFolderRepository userFolderRepository)
        {
            _userSessionService = userSessionService;
            _fileRepository = fileRepository;
            _userFolderRepository = userFolderRepository;
            _userFolderRepository = userFolderRepository;
        }

        public async Task<UserFile> SaveEncryptedFileAsync(IFormFile file, Guid? parentFolderId, Guid userId)
        {
            UserFolder? parentFolder;
            string folderVirtualPath;

            if (parentFolderId.HasValue)
            {
                parentFolder = await _userFolderRepository.GetFolderByIdAsync(parentFolderId.Value)
                              ?? throw new InvalidOperationException("Parent folder not found.");
                folderVirtualPath = parentFolder.VirtualPath;
            }
            else
            {
                parentFolder = null;
                folderVirtualPath = "";
            }

            var userKey = _userSessionService.GetEncryptionKey(userId);
            if (string.IsNullOrEmpty(userKey))
                throw new InvalidOperationException("Encryption key not found for the user.");

            var folderPath = Path.Combine(AppContext.BaseDirectory, "Documents", userId.ToString(), folderVirtualPath);
            Directory.CreateDirectory(folderPath);

            var fileId = Guid.NewGuid();
            var storedFileName = $"{fileId}.dat";
            var fullPhysicalPath = Path.Combine(folderPath, storedFileName);

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            byte[] fileBytes = memoryStream.ToArray();

            string contentType = file.ContentType;

            if (contentType.StartsWith("image/") || contentType.StartsWith("video/"))
            {
                var thumbnailFolderPath = Path.Combine(folderPath, ".thumbnails");
                Directory.CreateDirectory(thumbnailFolderPath);
                var thumbnailPath = Path.Combine(thumbnailFolderPath, $"{fileId}.png");

                GeneratorHelper.GenerateThumbnail(fileBytes, contentType, thumbnailPath);
            }

            byte[] encryptedData = EncryptionHelper.EncryptFileBytes(fileBytes, userKey);
            await File.WriteAllBytesAsync(fullPhysicalPath, encryptedData);

            var userFile = new UserFile
            {
                Id = fileId,
                OriginalName = file.FileName,
                Name = storedFileName,
                FilePath = Path.Combine("Documents", userId.ToString(), folderVirtualPath, storedFileName).Replace("\\", "/"),
                Size = file.Length,
                ContentType = contentType,
                UserId = userId,
                FolderId = parentFolder?.Id
            };

            await _fileRepository.AddFileAsync(userFile);

            return userFile;
        }

        public async Task<(byte[] Data, string FileName, string ContentType)> GetDecryptedFilesAsync(List<Guid> fileIds, Guid userId)
        {
            var files = await _fileRepository.GetFilesByIdsAsync(fileIds);
            var userKey = _userSessionService.GetEncryptionKey(userId);

            if (string.IsNullOrEmpty(userKey))
                throw new InvalidOperationException("Encryption key not found for the user.");

            if (files.Count == 1)
            {
                var file = files.First();
                var path = Path.Combine(AppContext.BaseDirectory, file.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                var encryptedBytes = await File.ReadAllBytesAsync(path);
                var decryptedBytes = EncryptionHelper.DecryptFileBytes(encryptedBytes, userKey);

                return (decryptedBytes, file.OriginalName, file.ContentType);
            }

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    var path = Path.Combine(AppContext.BaseDirectory, file.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (!File.Exists(path)) continue;

                    var encryptedBytes = await File.ReadAllBytesAsync(path);
                    var decryptedBytes = EncryptionHelper.DecryptFileBytes(encryptedBytes, userKey);

                    var entry = archive.CreateEntry(file.OriginalName, CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(decryptedBytes, 0, decryptedBytes.Length);
                }
            }

            memoryStream.Position = 0;
            return (memoryStream.ToArray(), "files.zip", "application/zip");
        }
    }
}
