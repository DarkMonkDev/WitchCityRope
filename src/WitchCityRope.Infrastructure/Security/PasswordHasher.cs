using WitchCityRope.Core.Interfaces;

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

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
        }
    }
}