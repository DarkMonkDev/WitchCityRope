namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// Interface for password hashing operations in the Auth feature
    /// </summary>
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
    }
}