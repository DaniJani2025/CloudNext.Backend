namespace CloudNext.DTOs.UserFolder
{
    public class FolderTreeDto
    {
        public Guid FolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string VirtualPath { get; set; } = string.Empty;
        public List<FolderTreeDto> SubFolders { get; set; } = new();
    }
}
