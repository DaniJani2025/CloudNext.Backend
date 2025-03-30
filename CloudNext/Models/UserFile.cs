namespace CloudNext.Models
{
    public class UserFile : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;

        public Guid UserId { get; set; } // Foreign Key
        public Guid? FolderId { get; set; }

        public User? User { get; set; }
        public UserFolder? Folder { get; set; }
    }

}
