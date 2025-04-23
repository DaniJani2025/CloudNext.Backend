namespace CloudNext.Common
{
    public class Constants
    {
        public static class Media
        {
            public static readonly HashSet<string> SupportedImageTypes = new()
            {
                "image/png",
                "image/jpeg",
                "image/jpg",
                "image/bmp",
                "image/gif",
                "image/tiff",
                "image/webp"
            };
        }
    }
}
