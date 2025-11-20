using WitchCityRope.Api.Features.Safety.Services;

namespace WitchCityRope.Api.Features.Shared.Services;

/// <summary>
/// Mock encryption service for integration testing
/// Does NOT actually encrypt - returns values as-is for testing simplicity
/// </summary>
public class MockEncryptionService : IEncryptionService
{
    /// <summary>
    /// Mock encryption - returns plaintext as-is
    /// Allows tests to use simple hardcoded values like "encrypted-capture-id"
    /// </summary>
    public Task<string> EncryptAsync(string plaintext)
    {
        return Task.FromResult(plaintext);
    }

    /// <summary>
    /// Mock decryption - returns ciphertext as-is
    /// Allows tests to use simple hardcoded values without actual encryption
    /// </summary>
    public Task<string> DecryptAsync(string ciphertext)
    {
        return Task.FromResult(ciphertext);
    }
}
