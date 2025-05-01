using CloudNext.Data;
using CloudNext.Interfaces;
using CloudNext.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudNext.Repositories
{
    public class UserFolderRepository : IUserFolderRepository
    {
        private readonly CloudNextDbContext _context;

        public UserFolderRepository(CloudNextDbContext context)
        {
            _context = context;
        }

        public async Task<UserFolder?> GetFolderAsync(Guid userId, Guid? parentFolderId, string folderName)
        {
            return await _context.UserFolders.FirstOrDefaultAsync(f =>
                f.UserId == userId &&
                f.ParentFolderId == parentFolderId &&
                f.Name.ToLower() == folderName.ToLower());
        }

        public async Task AddFolderAsync(UserFolder folder)
        {
            _context.UserFolders.Add(folder);
            await _context.SaveChangesAsync();
        }

        public async Task<UserFolder?> GetFolderByIdAsync(Guid folderId)
        {
            return await _context.UserFolders.FirstOrDefaultAsync(f => f.Id == folderId);
        }

        public async Task<UserFolder?> GetRootFolderAsync(Guid userId)
        {
            return await _context.UserFolders
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ParentFolderId == null);
        }

        public async Task<List<UserFolder>> GetFoldersByParentIdAsync(Guid userId, Guid parentId)
        {
            return await _context.UserFolders
                .Where(f => f.UserId == userId && f.ParentFolderId == parentId)
                .ToListAsync();
        }
    }
}
