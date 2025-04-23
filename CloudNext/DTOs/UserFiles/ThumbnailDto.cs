namespace CloudNext.DTOs.UserFiles
{
    public class ThumbnailDto
    {
        public Guid FileId { get; set; }
        public required string OriginalName { get; set; }
        public required string Base64Thumbnail { get; set; }
    }
}
