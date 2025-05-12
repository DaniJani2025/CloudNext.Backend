using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using System.Security.Cryptography;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using CloudNext.Common;

namespace CloudNext.Utils
{
    public class GeneratorHelper
    {
        private static readonly int _keySize = EncryptionConfig.KeySize;
        private static readonly Random rand = new();

        public static string GenerateRegistrationUrl(string email, IConfiguration configuration)
        {
            string token = JwtTokenHelper.GenerateRegistrationToken(email, configuration);
            string apiBaseUrl = configuration["AppSettings:ApiBaseUrl"]
                ?? throw new InvalidOperationException("Registration base URL is not configured.");

            return $"{apiBaseUrl}/api/users/verify?token={token}";
        }

        public static string GenerateEncryptionKey(IConfiguration configuration)
        {
            byte[] keyBytes = new byte[_keySize];
            RandomNumberGenerator.Fill(keyBytes);
            return Convert.ToHexString(keyBytes);
        }

        public static string GenerateRecoveryKey(IConfiguration configuration)
        {
            string upperCaseChars = configuration["PasswordStrings:UpperCaseAlphabets"]!;
            string lowerCaseChars = configuration["PasswordStrings:LowerCaseAlphabets"]!;
            string digits = configuration["PasswordStrings:Digits"]!;

            string allChars = upperCaseChars + lowerCaseChars + digits;

            var keyChars = new char[_keySize];
            Random rand = new();

            for (int i = 0; i < _keySize; i++)
            {
                keyChars[i] = allChars[rand.Next(allChars.Length)];
            }

            return new string(keyChars);
        }

        public async static Task GenerateThumbnail(byte[] fileBytes, string contentType, string thumbnailPath)
        {
            if (contentType.StartsWith("image/"))
            {
                if (!Constants.Media.SupportedImageTypes.Contains(contentType.ToLower()))
                    return;

                using var image = SixLabors.ImageSharp.Image.Load(fileBytes);
                var thumbnail = image.Clone(ctx => ctx.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(300, 300)
                }));
                thumbnail.Save(thumbnailPath);
            }
            else if (contentType.StartsWith("video/"))
            {
                // strip any “; codecs=” suffix
                var mediaType = contentType.Split(';')[0].Trim().ToLowerInvariant();
                if (!Constants.Media.SupportedVideoTypes.Contains(mediaType))
                    return;

                // write to temp file with correct extension
                var ext = mediaType switch
                {
                    "video/mp4" => ".mp4",
                    "video/avi" => ".avi",
                    _ => Path.GetExtension(thumbnailPath)
                };
                var tempVideoPath = Path.ChangeExtension(Path.GetTempFileName(), ext);
                File.WriteAllBytes(tempVideoPath, fileBytes);

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",   // or full path
                        Arguments = $"-hide_banner -loglevel error -i \"{tempVideoPath}\" -ss 00:00:01.000 -vframes 1 \"{thumbnailPath}\" -y",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo)!;
                    // read the single stderr stream so we don’t deadlock
                    var error = await process.StandardError.ReadToEndAsync();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                        throw new InvalidOperationException($"FFmpeg error (code {process.ExitCode}): {error}");
                }
                finally
                {
                    File.Delete(tempVideoPath);
                }
            }
        }
    }
}
