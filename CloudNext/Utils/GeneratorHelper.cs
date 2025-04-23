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
        private static readonly int _keySize = 32;
        private static readonly int _iterations = 100000;
        private static readonly Random rand = new();

        public static string GenerateRegistrationUrl(string email, IConfiguration configuration)
        {
            string token = JwtTokenHelper.GenerateRegistrationToken(email, configuration);
            string baseUrl = configuration["AppSettings:BaseUrl"]
                ?? throw new InvalidOperationException("Registration base URL is not configured.");

            return $"{baseUrl}/api/users/verify?token={token}";
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

            var keyChars = new char[32];
            Random rand = new();

            for (int i = 0; i < 32; i++)
            {
                keyChars[i] = allChars[rand.Next(allChars.Length)];
            }

            return new string(keyChars);
        }

        public static string DeriveKeyFromPassword(string password, string saltHex)
        {
            byte[] salt = Convert.FromHexString(saltHex);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, _iterations, HashAlgorithmName.SHA256);
            return Convert.ToHexString(pbkdf2.GetBytes(_keySize));
        }

        public static void GenerateThumbnail(byte[] fileBytes, string contentType, string thumbnailPath)
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
                var tempVideoPath = Path.GetTempFileName();
                File.WriteAllBytes(tempVideoPath, fileBytes);

                var outputThumbnailPath = thumbnailPath;
                var ffmpegPath = "ffmpeg";

                var startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-i \"{tempVideoPath}\" -ss 00:00:01.000 -vframes 1 \"{outputThumbnailPath}\" -y",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process.WaitForExit();

                File.Delete(tempVideoPath);
            }
        }
    }
}
