using WitchCityRope.Core.Interfaces;

namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// Adapter to make Core.Interfaces.IEncryptionService work with Auth.Services.IEncryptionService interface
    /// </summary>
    public class EncryptionServiceAdapter : IEncryptionService
    {
        private readonly Core.Interfaces.IEncryptionService _encryptionService;

        public EncryptionServiceAdapter(Core.Interfaces.IEncryptionService encryptionService)
        {
            _encryptionService = encryptionService;
        }

        public string Encrypt(string plainText)
        {
            // The Core encryption service uses async methods, but Auth expects sync
            // For now, we'll use GetAwaiter().GetResult() - not ideal but works
            return _encryptionService.EncryptAsync(plainText).GetAwaiter().GetResult();
        }

        public string Decrypt(string cipherText)
        {
            return _encryptionService.DecryptAsync(cipherText).GetAwaiter().GetResult();
        }

        public byte[] Encrypt(byte[] data)
        {
            // Convert byte array to base64 string, encrypt, then convert back
            var plainText = System.Convert.ToBase64String(data);
            var encryptedText = Encrypt(plainText);
            return System.Text.Encoding.UTF8.GetBytes(encryptedText);
        }

        public byte[] Decrypt(byte[] encryptedData)
        {
            // Convert encrypted bytes to string, decrypt, then convert back from base64
            var encryptedText = System.Text.Encoding.UTF8.GetString(encryptedData);
            var decryptedText = Decrypt(encryptedText);
            return System.Convert.FromBase64String(decryptedText);
        }
    }
}