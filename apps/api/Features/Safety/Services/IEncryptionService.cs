namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// AES-256 encryption service for sensitive safety data
/// </summary>
public interface IEncryptionService
{
    Task<string> EncryptAsync(string plainText);
    Task<string> DecryptAsync(string encryptedText);
}