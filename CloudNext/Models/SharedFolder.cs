namespace CloudNext.Models
{
    public class SharedFolder : BaseEntity
    {
        public Guid FolderId { get; set; }  // Foreign Key
        public Guid OwnerId { get; set; }
        public Guid SharedWithUserId { get; set; }

        public UserFolder? Folder { get; set; }
        public User? Owner { get; set; }
        public User? SharedWithUser { get; set; }
    }
}
