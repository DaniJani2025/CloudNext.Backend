using System.Security.Cryptography;
using System.Text;

namespace CloudNext.Utils
{
    public static class EncryptionHelper
    {
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

        public static byte[] EncryptFileBytes(byte[] fileBytes, string hexKey)
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromHexString(hexKey);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] encryptedContent = encryptor.TransformFinalBlock(fileBytes, 0, fileBytes.Length);

            byte[] result = new byte[aes.IV.Length + encryptedContent.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedContent, 0, result, aes.IV.Length, encryptedContent.Length);

            return result;
        }
    }
}
