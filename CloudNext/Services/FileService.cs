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
        private readonly IStorageService _storageService;

        public FileService(
            IUserSessionService userSessionService, 
            IUserFileRepository fileRepository, 
            IUserFolderRepository userFolderRepository,
            IStorageService storageService)
        {
            _userSessionService = userSessionService;
            _fileRepository = fileRepository;
            _userFolderRepository = userFolderRepository;
            _storageService = storageService;
        }

        public async Task<UserFile> SaveEncryptedFileAsync(
           IFormFile file,
           Guid? parentFolderId,
           Guid userId)
        {
            UserFolder? parentFolder;
            string folderVirtualPath;

            if (parentFolderId.HasValue)
            {
                parentFolder = await _userFolderRepository
                    .GetFolderByIdAsync(parentFolderId.Value)
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
                throw new InvalidOperationException("Encryption key not found.");

            var fileId = Guid.NewGuid();
            var storedFileName = $"{fileId}.dat";

            var objectKey = Path.Combine(
                userId.ToString(),
                folderVirtualPath,
                storedFileName
            ).Replace("\\", "/");

            var contentType = file.ContentType;

            // -------- Thumbnail Section --------
            if (!string.IsNullOrEmpty(contentType) &&
                (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                 contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)))
            {
                using var thumbStream = file.OpenReadStream();
                using var memory = new MemoryStream();

                await thumbStream.CopyToAsync(memory);

                var thumbnailBytes = await GeneratorHelper
                    .GenerateThumbnailBytes(memory.ToArray(), contentType);

                if (thumbnailBytes != null)
                {
                    var thumbnailKey = Path.Combine(
                        userId.ToString(),
                        folderVirtualPath,
                        ".thumbnails",
                        $"{fileId}.png"
                    ).Replace("\\", "/");

                    using var thumbnailStream = new MemoryStream(thumbnailBytes);
                    await _storageService.SaveAsync(thumbnailStream, thumbnailKey);
                }
            }

            // -------- Encryption Section --------
            using var inputStream = file.OpenReadStream();
            using var encryptedOutput = new MemoryStream();

            await EncryptionHelper.EncryptToStreamAsync(
                inputStream,
                encryptedOutput,
                userKey);

            encryptedOutput.Position = 0;

            await _storageService.SaveAsync(encryptedOutput, objectKey);

            var userFile = new UserFile
            {
                Id = fileId,
                OriginalName = file.FileName,
                Name = storedFileName,
                FilePath = objectKey,
                Size = file.Length,
                ContentType = contentType,
                UserId = userId,
                FolderId = parentFolder?.Id
            };

            await _fileRepository.AddFileAsync(userFile);

            return userFile;
        }

        public async Task<(byte[] Data, string FileName, string ContentType)>
            GetDecryptedFilesAsync(List<Guid> fileIds, Guid userId)
        {
            var files = await _fileRepository.GetFilesByIdsAsync(fileIds);
            var userKey = await _userSessionService.GetEncryptionKey(userId);

            if (string.IsNullOrEmpty(userKey))
                throw new InvalidOperationException("Encryption key not found.");

            if (files.Count == 1)
            {
                var file = files.First();

                using var encryptedStream =
                    await _storageService.GetAsync(file.FilePath);

                using var decryptedStream = new MemoryStream();

                await EncryptionHelper.DecryptToStreamAsync(
                    encryptedStream,
                    decryptedStream,
                    userKey);

                return (
                    decryptedStream.ToArray(),
                    file.OriginalName,
                    file.ContentType
                );
            }

            using var zipMemory = new MemoryStream();

            using (var archive = new ZipArchive(zipMemory, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    using var encryptedStream =
                        await _storageService.GetAsync(file.FilePath);

                    var entry = archive.CreateEntry(
                        file.OriginalName,
                        CompressionLevel.Fastest);

                    using var entryStream = entry.Open();

                    await EncryptionHelper.DecryptToStreamAsync(
                        encryptedStream,
                        entryStream,
                        userKey);
                }
            }

            zipMemory.Position = 0;

            return (
                zipMemory.ToArray(),
                "files.zip",
                "application/zip"
            );
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

        public async Task<FileStreamWithMetadataDto> StreamDecryptedVideoAsync(
            Guid fileId,
            string userId,
            string rangeHeader)
        {
            var file = await _fileRepository.GetFileByIdAsync(fileId);

            if (file == null || file.UserId.ToString() != userId)
                throw new FileNotFoundException("File not found or access denied.");

            var userKey = await _userSessionService.GetEncryptionKey(Guid.Parse(userId));
            if (string.IsNullOrEmpty(userKey))
                throw new UnauthorizedAccessException("Encryption key not found.");

            using var encryptedStream =
                await _storageService.GetAsync(file.FilePath);

            var decryptedStream = new MemoryStream();

            await EncryptionHelper.DecryptToStreamAsync(
                encryptedStream,
                decryptedStream,
                userKey);

            decryptedStream.Position = 0;

            long totalLength = decryptedStream.Length;
            long start = 0;
            long end = totalLength - 1;

            if (!string.IsNullOrEmpty(rangeHeader) &&
                rangeHeader.StartsWith("bytes="))
            {
                var range = rangeHeader.Substring(6).Split('-');

                if (long.TryParse(range[0], out var parsedStart))
                    start = parsedStart;

                if (range.Length > 1 &&
                    long.TryParse(range[1], out var parsedEnd))
                    end = parsedEnd;
            }

            end = Math.Min(end, totalLength - 1);
            long contentLength = end - start + 1;

            decryptedStream.Position = start;

            return new FileStreamWithMetadataDto
            {
                Stream = new SubStream(decryptedStream, contentLength),
                ContentType = file.ContentType,
                ContentLength = contentLength,
                ContentRange = $"bytes {start}-{end}/{totalLength}"
            };
        }
    }
}
