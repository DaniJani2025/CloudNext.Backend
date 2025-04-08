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

        public async Task<UserFile> SaveEncryptedFileAsync(IFormFile file, Guid userId, Guid? folderId = null)
        {
            var userKey = _userSessionService.GetEncryptionKey(userId);
            if (string.IsNullOrEmpty(userKey))
                throw new InvalidOperationException("Encryption key not found for the user.");

            var uploadsRoot = Path.Combine(AppContext.BaseDirectory, "Documents", userId.ToString());
            Directory.CreateDirectory(uploadsRoot);

            var ext = Path.GetExtension(file.FileName);
            var uniqueName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(file.FileName)}{ext}";
            var fullPath = Path.Combine(uploadsRoot, uniqueName);

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            byte[] encryptedData = EncryptionHelper.EncryptFileBytes(memoryStream.ToArray(), userKey);
            await File.WriteAllBytesAsync(fullPath, encryptedData);

            var userFile = new UserFile
            {
                Name = file.FileName,
                FilePath = Path.Combine("Documents", userId.ToString(), uniqueName).Replace("\\", "/"),
                Size = file.Length,
                ContentType = file.ContentType,
                UserId = userId,
                FolderId = folderId
            };

            _context.UserFiles.Add(userFile);
            await _context.SaveChangesAsync();

            return userFile;
        }
    }
}
