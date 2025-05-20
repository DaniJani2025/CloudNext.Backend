using System.IO.Compression;
using CloudNext.Data;
using CloudNext.DTOs.UserFiles;
using CloudNext.Interfaces;
using CloudNext.Models;
using CloudNext.Repositories;
using CloudNext.Utils;
using CloudNext.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloudNext.Services
{
    public class FileService : IFileService
    {
        private readonly IUserSessionService _userSessionService;
        private readonly IUserFileRepository _fileRepository;
        private readonly IUserFolderRepository _userFolderRepository;

        public FileService(IUserSessionService userSessionService, IUserFileRepository fileRepository, IUserFolderRepository userFolderRepository)
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

            var userKey = await _userSessionService.GetEncryptionKey(userId);
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

                await GeneratorHelper.GenerateThumbnail(fileBytes, contentType, thumbnailPath);
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
            var userKey = await _userSessionService.GetEncryptionKey(userId);

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

        public async Task<List<ThumbnailDto>> GetThumbnailsForFolderAsync(Guid? folderId, Guid userId)
        {
            string folderVirtualPath;
            List<UserFile> files;

            if (folderId.HasValue)
            {
                var folder = await _userFolderRepository.GetFolderByIdAsync(folderId.Value)
                             ?? throw new InvalidOperationException("Folder not found.");

                folderVirtualPath = folder.VirtualPath;
                files = await _fileRepository.GetFilesByFolderIdAsync(folderId.Value);
            }
            else
            {
                folderVirtualPath = "";
                files = await _fileRepository.GetFilesInRootAsync(userId);
            }

            var folderPath = Path.Combine(AppContext.BaseDirectory, "Documents", userId.ToString(), folderVirtualPath);
            var thumbnailFolderPath = Path.Combine(folderPath, ".thumbnails");

            var thumbnails = new List<ThumbnailDto>();

            foreach (var file in files)
            {
                string? base64Thumbnail = null;

                if (Constants.Media.SupportedImageTypes.Contains(file.ContentType) ||
                    Constants.Media.SupportedVideoTypes.Contains(file.ContentType))
                {
                    var thumbPath = Path.Combine(thumbnailFolderPath, $"{file.Id}.png");
                    if (System.IO.File.Exists(thumbPath))
                    {
                        var imageBytes = await System.IO.File.ReadAllBytesAsync(thumbPath);
                        base64Thumbnail = $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
                    }
                }
                else
                {
                    var ext = Path.GetExtension(file.OriginalName)?.ToLower();
                    if (ext != null && Constants.Media.CommonFileLogos.TryGetValue(ext, out var logoFile))
                    {
                        var logoPath = Path.Combine(AppContext.BaseDirectory, "Documents", "CommonThumbnails", logoFile);
                        if (System.IO.File.Exists(logoPath))
                        {
                            var logoBytes = await System.IO.File.ReadAllBytesAsync(logoPath);
                            base64Thumbnail = $"data:image/png;base64,{Convert.ToBase64String(logoBytes)}";
                        }
                    }
                }

                if (base64Thumbnail != null)
                {
                    thumbnails.Add(new ThumbnailDto
                    {
                        FileId = file.Id,
                        OriginalName = file.OriginalName,
                        Base64Thumbnail = base64Thumbnail
                    });
                }
            }

            return thumbnails;
        }

        public async Task<FileStreamWithMetadataDto> StreamDecryptedVideoAsync(Guid fileId, string userId, string rangeHeader)
        {
            var file = await _fileRepository.GetFileByIdAsync(fileId);
            if (file == null || file.UserId.ToString() != userId)
                throw new FileNotFoundException("File not found or access denied.");

            var userKey = await _userSessionService.GetEncryptionKey(Guid.Parse(userId));
            if (string.IsNullOrEmpty(userKey))
                throw new UnauthorizedAccessException("Encryption key not found.");

            var path = Path.Combine(AppContext.BaseDirectory, file.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (!System.IO.File.Exists(path))
                throw new FileNotFoundException("Encrypted file not found on disk.");

            var encryptedBytes = await System.IO.File.ReadAllBytesAsync(path);
            var decryptedBytes = EncryptionHelper.DecryptFileBytes(encryptedBytes, userKey);

            long totalLength = decryptedBytes.Length;
            long start = 0;
            long end = totalLength - 1;

            if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes="))
            {
                var range = rangeHeader.Substring("bytes=".Length).Split('-');
                if (long.TryParse(range[0], out long parsedStart)) start = parsedStart;
                if (range.Length > 1 && long.TryParse(range[1], out long parsedEnd)) end = parsedEnd;
            }

            end = Math.Min(end, totalLength - 1);
            long contentLength = end - start + 1;

            var stream = new MemoryStream(decryptedBytes, (int)start, (int)contentLength);

            return new FileStreamWithMetadataDto
            {
                Stream = stream,
                ContentType = file.ContentType,
                ContentLength = contentLength,
                ContentRange = $"bytes {start}-{end}/{totalLength}"
            };
        }
    }
}
