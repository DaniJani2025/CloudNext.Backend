namespace CloudNext.DTOs.UserFolder
{
    public class CreateFolderDto
    {
        public Guid UserId { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public Guid? ParentFolderId { get; set; }
    }
}
