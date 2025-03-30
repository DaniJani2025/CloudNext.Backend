namespace CloudNext.Models
{
    public class SharedFile : BaseEntity
    {
        public Guid FileId { get; set; }  // Foreign Key
        public Guid OwnerId { get; set; }
        public Guid SharedWithUserId { get; set; }

        public UserFile? File { get; set; }
        public User? Owner { get; set; }
        public User? SharedWithUser { get; set; }
    }
}
