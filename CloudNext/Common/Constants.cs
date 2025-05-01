namespace CloudNext.Common
{
    public class Constants
    {
        public static class Token
        {
            public const int TokenExpirationHours = 24;
            public const int RefreshExpirationDays = 7;
        }

        public static class Media
        {
            public static readonly HashSet<string> SupportedImageTypes = new()
            {
                "image/png", "image/jpeg", "image/jpg", "image/bmp", "image/gif", "image/tiff", "image/webp"
            };

            public static readonly HashSet<string> SupportedVideoTypes = new()
            {
                "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska"
            };

            public static readonly Dictionary<string, string> CommonFileLogos = new()
            {
                { ".avif", "image.png" },
                { ".heic", "image.png" },
                { ".heif", "image.png" },
                { ".ico", "image.png" },
                { ".jfif", "image.png" },
                { ".svg", "image.png" },

                { ".pdf", "pdf.png" },
                { ".docx", "word.png" },
                { ".doc", "word.png" },
                { ".odt", "word.png" },
                { ".rtf", "word.png" },

                { ".xlsx", "excel.png" },
                { ".xls", "excel.png" },
                { ".csv", "excel.png" },
                { ".ods", "excel.png" },

                { ".pptx", "ppt.png" },
                { ".ppt", "ppt.png" },
                { ".odp", "ppt.png" },

                { ".zip", "zip.png" },
                { ".rar", "zip.png" },
                { ".7z", "zip.png" },
                { ".tar", "zip.png" },
                { ".gz", "zip.png" },

                { ".txt", "text.png" },
                { ".log", "text.png" },
                { ".md", "text.png" },

                { ".json", "code.png" },
                { ".xml", "code.png" },
                { ".html", "code.png" },
                { ".css", "code.png" },
                { ".js", "code.png" },
                { ".ts", "code.png" },
                { ".cs", "code.png" },
                { ".java", "code.png" },
                { ".py", "code.png" },

                { ".mp3", "audio.png" },
                { ".wav", "audio.png" },
                { ".flac", "audio.png" }
            };
        }

        public const long MaxUploadSizeInBytes = 10L * 1024 * 1024 * 1024;
    }
}
