using System.Text;
using WitchCityRope.Tests.Common.Interfaces;

namespace WitchCityRope.Tests.Common.TestDoubles
{
    /// <summary>
    /// Test implementation of IEncryptionService that provides simple reversible encryption for testing
    /// </summary>
    public class TestEncryptionService : IEncryptionService
    {
        private const string TestPrefix = "TEST_ENCRYPTED:";
        
        public bool SimulateFailure { get; set; }
        public bool UseRealEncryption { get; set; }

        public string Encrypt(string plainText)
        {
            if (SimulateFailure)
                throw new InvalidOperationException("Encryption service is configured to fail");

            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            if (UseRealEncryption)
            {
                // Use base64 encoding for test purposes
                var bytes = Encoding.UTF8.GetBytes(plainText);
                return TestPrefix + Convert.ToBase64String(bytes);
            }

            // Simple test encryption - just add prefix
            return TestPrefix + plainText;
        }

        public string Decrypt(string cipherText)
        {
            if (SimulateFailure)
                throw new InvalidOperationException("Encryption service is configured to fail");

            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            if (!cipherText.StartsWith(TestPrefix))
                throw new ArgumentException("Invalid encrypted data");

            var encrypted = cipherText.Substring(TestPrefix.Length);

            if (UseRealEncryption)
            {
                // Decode from base64
                var bytes = Convert.FromBase64String(encrypted);
                return Encoding.UTF8.GetString(bytes);
            }

            // Simple test decryption - just remove prefix
            return encrypted;
        }

        public byte[] Encrypt(byte[] data)
        {
            if (SimulateFailure)
                throw new InvalidOperationException("Encryption service is configured to fail");

            if (data == null || data.Length == 0)
                return Array.Empty<byte>();

            // For testing, just XOR with a simple key
            var key = new byte[] { 0x42, 0x43, 0x44, 0x45 };
            var result = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ key[i % key.Length]);
            }

            return result;
        }

        public byte[] Decrypt(byte[] encryptedData)
        {
            if (SimulateFailure)
                throw new InvalidOperationException("Encryption service is configured to fail");

            if (encryptedData == null || encryptedData.Length == 0)
                return Array.Empty<byte>();

            // XOR decryption (same as encryption)
            return Encrypt(encryptedData);
        }

        /// <summary>
        /// Helper method to check if a string was encrypted by this service
        /// </summary>
        public bool IsEncrypted(string value)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith(TestPrefix);
        }
    }
}