using CloudNext.Data;
using CloudNext.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudNext.Repositories.Users
{
    public class FileRepository : IFileRepository
    {
        private readonly CloudNextDbContext _context;

        public FileRepository(CloudNextDbContext context)
        {
            _context = context;
        }

        public async Task AddFileAsync(UserFile file)
        {
            _context.UserFiles.Add(file);
            await _context.SaveChangesAsync();
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
    }

}
