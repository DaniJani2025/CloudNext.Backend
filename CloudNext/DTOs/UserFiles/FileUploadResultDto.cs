namespace CloudNext.DTOs.UserFiles
{
    public class FileUploadResultDto
    {
        public Guid FileId { get; set; }
        public string OriginalName { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}
