using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using WitchCityRope.Core.Interfaces;
using BCrypt.Net;

namespace WitchCityRope.Infrastructure.Security
{
    /// <summary>
    /// Encryption service implementation using AES for encryption and BCrypt for hashing
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        private readonly string _encryptionKey;
        private readonly byte[] _keyBytes;
        private readonly byte[] _ivBytes;

        public EncryptionService(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _encryptionKey = configuration["Encryption:Key"];
            if (string.IsNullOrEmpty(_encryptionKey))
            {
                throw new InvalidOperationException("Encryption key not configured");
            }

            // Derive key and IV from the configured key
            using (var sha256 = SHA256.Create())
            {
                var fullKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(_encryptionKey));
                _keyBytes = new byte[32]; // 256 bits for AES-256
                Array.Copy(fullKey, 0, _keyBytes, 0, 32);

                // Generate IV from the key (in production, consider using random IV per encryption)
                var ivHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(_encryptionKey + ":IV"));
                _ivBytes = new byte[16]; // 128 bits for AES block size
                Array.Copy(ivHash, 0, _ivBytes, 0, 16);
            }
        }

        public async Task<string> EncryptAsync(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            return await Task.Run(() =>
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _keyBytes;
                    aes.IV = _ivBytes;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }

                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            });
        }

        public async Task<string> DecryptAsync(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            return await Task.Run(() =>
            {
                try
                {
                    var cipherBytes = Convert.FromBase64String(encryptedText);

                    using (var aes = Aes.Create())
                    {
                        aes.Key = _keyBytes;
                        aes.IV = _ivBytes;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using (var decryptor = aes.CreateDecryptor())
                        using (var msDecrypt = new MemoryStream(cipherBytes))
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
                catch (Exception)
                {
                    // Log the exception
                    throw new InvalidOperationException("Failed to decrypt data");
                }
            });
        }

        public string Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentNullException(nameof(input));

            // Using BCrypt for secure password hashing
            return BCrypt.Net.BCrypt.HashPassword(input, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        public bool VerifyHash(string plainText, string hash)
        {
            if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(plainText, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}