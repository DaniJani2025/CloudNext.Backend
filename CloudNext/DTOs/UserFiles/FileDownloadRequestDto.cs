namespace CloudNext.DTOs.UserFiles
{
    public class FileDownloadRequestDto
    {
        public Guid UserId { get; set; }
        public List<Guid> FileIds { get; set; } = new();
    }
}
