namespace CloudNext.DTOs.UserFiles
{
    public class FileStreamWithMetadataDto
    {
        public Stream Stream { get; set; }
        public string ContentType { get; set; }
        public long ContentLength { get; set; }
        public string ContentRange { get; set; }
    }

}
