using CloudNext.Data;
using CloudNext.Interfaces;
using CloudNext.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudNext.Repositories
{
    public class UserFileRepository : IUserFileRepository
    {
        private readonly CloudNextDbContext _context;

        public UserFileRepository(CloudNextDbContext context)
        {
            _context = context;
        }

        public async Task AddFileAsync(UserFile file)
        {
            _context.UserFiles.Add(file);
            await _context.SaveChangesAsync();
        }

        public async Task<UserFile?> GetFileByIdAsync(Guid fileId)
        {
            return await _context.UserFiles
                .FirstOrDefaultAsync(f => f.Id == fileId);
        }

        public async Task<List<UserFile>> GetFilesByIdsAsync(List<Guid> fileIds)
        {
            return await _context.UserFiles
                .Where(f => fileIds.Contains(f.Id))
                .ToListAsync();
        }

        public async Task<List<UserFile>> GetFilesByFolderIdAsync(Guid folderId)
        {
            return await _context.UserFiles
                .Where(f => f.FolderId == folderId)
                .ToListAsync();
        }

        public async Task<List<UserFile>> GetFilesInRootAsync(Guid userId)
        {
            return await _context.UserFiles
                .Where(f => f.UserId == userId && f.FolderId == null)
                .ToListAsync();
        }
    }

}
