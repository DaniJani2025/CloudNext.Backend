namespace CloudNext.DTOs.UserFolder
{
    public class FolderResponseDto
    {
        public Guid FolderId { get; set; }
        public string Name { get; set; } = default!;
        public string VirtualPath { get; set; } = default!;
        public Guid? ParentFolderId { get; set; }
    }
}
