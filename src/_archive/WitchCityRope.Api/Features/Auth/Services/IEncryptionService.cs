namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// Interface for encryption operations in the Auth feature
    /// </summary>
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        byte[] Encrypt(byte[] data);
        byte[] Decrypt(byte[] encryptedData);
    }
}