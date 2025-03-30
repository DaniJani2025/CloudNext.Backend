namespace CloudNext.Models
{
    public class Trash : BaseEntity
    {
        public Guid? FileId { get; set; }  // Foreign Key (nullable)
        public Guid? FolderId { get; set; }  // Foreign Key (nullable)
        public Guid UserId { get; set; }
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;

        public UserFile? File { get; set; }
        public UserFolder? Folder { get; set; }
        public User? User { get; set; }
    }
}
