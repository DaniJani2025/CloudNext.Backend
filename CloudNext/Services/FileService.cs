using CloudNext.Data;
using CloudNext.Interfaces;
using CloudNext.Models;
using CloudNext.Utils;
using Microsoft.AspNetCore.Http;

namespace CloudNext.Services
{
    public class FileService : IFileService
    {
        private readonly CloudNextDbContext _context;
        private readonly IUserSessionService _userSessionService;

        public FileService(CloudNextDbContext context, IUserSessionService userSessionService)
        {
            _context = context;
            _userSessionService = userSessionService;
        }

        public async Task<UserFile> SaveEncryptedFileAsync(IFormFile file, Guid parentFolderId)
        {
            var parentFolder = await _context.UserFolders.FindAsync(parentFolderId)
                               ?? throw new InvalidOperationException("Parent folder not found.");

            var userId = parentFolder.UserId;
            var userKey = _userSessionService.GetEncryptionKey(userId);
            if (string.IsNullOrEmpty(userKey))
                throw new InvalidOperationException("Encryption key not found for the user.");

            var folderVirtualPath = parentFolder.VirtualPath;
            var folderPath = Path.Combine(AppContext.BaseDirectory, "Documents", userId.ToString(), folderVirtualPath);

            Directory.CreateDirectory(folderPath);

            var fileId = Guid.NewGuid();
            var storedFileName = $"{fileId}.dat";
            var fullPhysicalPath = Path.Combine(folderPath, storedFileName);

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            byte[] encryptedData = EncryptionHelper.EncryptFileBytes(memoryStream.ToArray(), userKey);
            await File.WriteAllBytesAsync(fullPhysicalPath, encryptedData);

            var userFile = new UserFile
            {
                Id = fileId,
                OriginalName = file.FileName,
                Name = storedFileName,
                FilePath = Path.Combine("Documents", userId.ToString(), folderVirtualPath, storedFileName).Replace("\\", "/"),
                Size = file.Length,
                ContentType = file.ContentType,
                UserId = userId,
                FolderId = parentFolder.Id
            };

            _context.UserFiles.Add(userFile);
            await _context.SaveChangesAsync();

            return userFile;
        }
    }
}
