namespace CloudNext.DTOs.UserFolder
{
    public class FolderUploadDto
    {
        public string ParentFolderId { get; set; } = string.Empty;
        public IFormFile ZipFile { get; set; } = null!;
    }
}
