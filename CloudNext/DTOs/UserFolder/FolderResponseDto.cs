namespace CloudNext.DTOs.UserFolder
{
    public class FolderResponseDto
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = default!;
        public string RelativePath { get; set; } = default!;
        public Guid? ParentFolderId { get; set; }
    }
}
