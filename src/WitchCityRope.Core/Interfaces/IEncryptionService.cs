using System.Threading.Tasks;

namespace WitchCityRope.Core.Interfaces
{
    /// <summary>
    /// Service for encrypting and decrypting sensitive data
    /// Used for protecting legal names and other PII
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts a plain text string
        /// </summary>
        /// <param name="plainText">The text to encrypt</param>
        /// <returns>Encrypted string</returns>
        Task<string> EncryptAsync(string plainText);

        /// <summary>
        /// Decrypts an encrypted string
        /// </summary>
        /// <param name="encryptedText">The encrypted text</param>
        /// <returns>Decrypted plain text</returns>
        Task<string> DecryptAsync(string encryptedText);

        /// <summary>
        /// Generates a hash of the input for comparison purposes
        /// </summary>
        /// <param name="input">The text to hash</param>
        /// <returns>Hashed string</returns>
        string Hash(string input);

        /// <summary>
        /// Verifies if a plain text matches a hash
        /// </summary>
        /// <param name="plainText">The plain text to verify</param>
        /// <param name="hash">The hash to compare against</param>
        /// <returns>True if matches, false otherwise</returns>
        bool VerifyHash(string plainText, string hash);
    }
}