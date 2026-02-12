using CloudNext.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudNext.Data
{
    public class CloudNextDbContext : DbContext
    {
        public CloudNextDbContext(DbContextOptions<CloudNextDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserFile> UserFiles { get; set; }
        public DbSet<UserFolder> UserFolders { get; set; }
        public DbSet<SharedFile> SharedFiles { get; set; }
        public DbSet<SharedFolder> SharedFolders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserFile>().HasQueryFilter(f => !f.IsDeleted);
            modelBuilder.Entity<UserFolder>().HasQueryFilter(f => !f.IsDeleted);

            // User-UserFolder one-to-many relationship
            modelBuilder.Entity<UserFolder>()
                .HasOne(f => f.User)
                .WithMany(u => u.Folders)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserFolder self-referencing for nested folders
            modelBuilder.Entity<UserFolder>()
                .HasOne(f => f.ParentFolder)
                .WithMany(f => f.SubFolders)
                .HasForeignKey(f => f.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents circular delete issues

            // User-UserFile one-to-many relationship
            modelBuilder.Entity<UserFile>()
                .HasOne(f => f.User)
                .WithMany(u => u.Files)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserFolder-UserFile one-to-many relationship
            modelBuilder.Entity<UserFile>()
                .HasOne(f => f.Folder)
                .WithMany(f => f.Files)
                .HasForeignKey(f => f.FolderId)
                .OnDelete(DeleteBehavior.SetNull);

            // SharedFile relationships
            modelBuilder.Entity<SharedFile>()
                .HasOne(sf => sf.File)
                .WithMany()
                .HasForeignKey(sf => sf.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SharedFile>()
                .HasOne(sf => sf.Owner)
                .WithMany()
                .HasForeignKey(sf => sf.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SharedFile>()
                .HasOne(sf => sf.SharedWithUser)
                .WithMany()
                .HasForeignKey(sf => sf.SharedWithUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // SharedFolder relationships
            modelBuilder.Entity<SharedFolder>()
                .HasOne(sf => sf.Folder)
                .WithMany(f => f.SharedWith)
                .HasForeignKey(sf => sf.FolderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SharedFolder>()
                .HasOne(sf => sf.Owner)
                .WithMany()
                .HasForeignKey(sf => sf.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SharedFolder>()
                .HasOne(sf => sf.SharedWithUser)
                .WithMany()
                .HasForeignKey(sf => sf.SharedWithUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
