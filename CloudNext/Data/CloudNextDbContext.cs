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
        public DbSet<SharedFolder> SharedFolders { get; set; } // Added SharedFolder
        public DbSet<Trash> Trashes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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

            // Trash relationships
            modelBuilder.Entity<Trash>()
                .HasOne(t => t.File)
                .WithMany()
                .HasForeignKey(t => t.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Trash>()
                .HasOne(t => t.Folder)
                .WithMany()
                .HasForeignKey(t => t.FolderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Trash>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
