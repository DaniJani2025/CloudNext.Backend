namespace CloudNext.Models
{
    public class UserFolder : SoftDeletableEntity
    {
        public string Name { get; set; } = string.Empty;
        public Guid UserId { get; set; }  // Owner Foreign Key
        public Guid? ParentFolderId { get; set; }  // For nested folders

        public User? User { get; set; }
        public UserFolder? ParentFolder { get; set; }
        public string VirtualPath { get; set; } = string.Empty;
        public List<UserFolder> SubFolders { get; set; } = new();
        public List<UserFile> Files { get; set; } = new();
        public List<SharedFolder> SharedWith { get; set; } = new();  // New
    }
}
