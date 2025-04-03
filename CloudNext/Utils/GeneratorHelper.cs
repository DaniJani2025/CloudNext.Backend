using System.Runtime.Intrinsics.Arm;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

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

        public static string DeriveKeyFromPassword(string password, IConfiguration configuration)
        {
            string randomSalt = configuration["Encryption:Salt"]!;
            byte[] salt = Encoding.UTF8.GetBytes(randomSalt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, _iterations, HashAlgorithmName.SHA256);
            return Convert.ToHexString(pbkdf2.GetBytes(_keySize));
        }
    }
}
