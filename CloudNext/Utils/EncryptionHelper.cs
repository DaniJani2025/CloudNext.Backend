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
    }
}
