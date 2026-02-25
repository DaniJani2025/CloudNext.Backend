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

        public async static Task<byte[]?> GenerateThumbnailBytes(byte[] fileBytes, string contentType)
        {
            if (contentType.StartsWith("image/"))
            {
                if (!Constants.Media.SupportedImageTypes.Contains(contentType.ToLower()))
                    return null;

                using var image = SixLabors.ImageSharp.Image.Load(fileBytes);
                using var outputStream = new MemoryStream();

                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(300, 300)
                }));

                await image.SaveAsPngAsync(outputStream);
                return outputStream.ToArray();
            }

            if (contentType.StartsWith("video/"))
            {
                var mediaType = contentType.Split(';')[0].Trim().ToLowerInvariant();
                if (!Constants.Media.SupportedVideoTypes.Contains(mediaType))
                    return null;

                var ext = mediaType switch
                {
                    "video/mp4" => ".mp4",
                    "video/avi" => ".avi",
                    _ => ".mp4"
                };

                var tempVideoPath = Path.ChangeExtension(Path.GetTempFileName(), ext);
                var tempThumbnailPath = Path.ChangeExtension(Path.GetTempFileName(), ".png");

                await File.WriteAllBytesAsync(tempVideoPath, fileBytes);

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-hide_banner -loglevel error -i \"{tempVideoPath}\" -ss 00:00:01.000 -vframes 1 \"{tempThumbnailPath}\" -y",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo)!;
                    var error = await process.StandardError.ReadToEndAsync();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                        throw new InvalidOperationException($"FFmpeg error (code {process.ExitCode}): {error}");

                    return await File.ReadAllBytesAsync(tempThumbnailPath);
                }
                finally
                {
                    if (File.Exists(tempVideoPath))
                        File.Delete(tempVideoPath);

                    if (File.Exists(tempThumbnailPath))
                        File.Delete(tempThumbnailPath);
                }
            }

            return null;
        }
    }
}
