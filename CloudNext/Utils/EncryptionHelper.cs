using System.Security.Cryptography;
using System.Text;

namespace CloudNext.Utils
{
    public static class EncryptionHelper
    {
        private static readonly int _iterations = 100_000;

        public static string EncryptData(string plainText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromHexString(key);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return Convert.ToBase64String(aes.IV) + ":" + Convert.ToBase64String(encryptedBytes);
        }

        public static string DecryptData(string encryptedData, string key)
        {
            var parts = encryptedData.Split(':');
            if (parts.Length != 2)
                throw new FormatException("Invalid encrypted data format.");

            var iv = Convert.FromBase64String(parts[0]);
            var encryptedBytes = Convert.FromBase64String(parts[1]);
            var keyBytes = Convert.FromHexString(key);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        public static string DeriveKeyFromPassword(string password, string saltHex)
        {
            int keySize = EncryptionConfig.KeySize;

            byte[] salt = Convert.FromHexString(saltHex);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, _iterations, HashAlgorithmName.SHA256);
            return Convert.ToHexString(pbkdf2.GetBytes(keySize));
        }

        public static async Task EncryptToStreamAsync(
            Stream inputStream,
            Stream outputStream,
            string hexKey)
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromHexString(hexKey);
            aes.GenerateIV();

            await outputStream.WriteAsync(aes.IV, 0, aes.IV.Length);

            using var cryptoStream = new CryptoStream(
                outputStream,
                aes.CreateEncryptor(aes.Key, aes.IV),
                CryptoStreamMode.Write,
                leaveOpen: true);

            await inputStream.CopyToAsync(cryptoStream);
            await cryptoStream.FlushFinalBlockAsync();
        }

        public static async Task DecryptToStreamAsync(
            Stream encryptedStream,
            Stream outputStream,
            string hexKey)
        {
            var key = Convert.FromHexString(hexKey);

            byte[] iv = new byte[16];
            await encryptedStream.ReadAsync(iv, 0, 16);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var cryptoStream = new CryptoStream(
                encryptedStream,
                aes.CreateDecryptor(),
                CryptoStreamMode.Read);

            await cryptoStream.CopyToAsync(outputStream);
        }
    }
}

