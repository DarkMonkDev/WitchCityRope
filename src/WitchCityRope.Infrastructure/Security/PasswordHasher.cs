using WitchCityRope.Api.Features.Auth.Services;

namespace WitchCityRope.Infrastructure.Security
{
    /// <summary>
    /// Implementation of password hashing using BCrypt
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}