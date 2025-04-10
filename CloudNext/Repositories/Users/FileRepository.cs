using CloudNext.Data;
using CloudNext.Models;

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
    }

}
