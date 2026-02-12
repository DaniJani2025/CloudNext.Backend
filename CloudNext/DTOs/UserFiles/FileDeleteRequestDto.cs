namespace CloudNext.DTOs.UserFiles
{
    public class FileDeleteRequestDto
    {
        public Guid UserId { get; set; }
        public List<Guid> FileIds { get; set; } = new();
    }
}
