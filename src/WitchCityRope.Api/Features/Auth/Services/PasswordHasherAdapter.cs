using WitchCityRope.Api.Services;

namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// Adapter to make the Api.Services.PasswordHasher work with Auth.Services.IPasswordHasher interface
    /// </summary>
    public class PasswordHasherAdapter : IPasswordHasher
    {
        private readonly Api.Services.IPasswordHasher _passwordHasher;

        public PasswordHasherAdapter(Api.Services.IPasswordHasher passwordHasher)
        {
            _passwordHasher = passwordHasher;
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            // Note: The parameter order is reversed in the underlying implementation
            return _passwordHasher.VerifyPassword(passwordHash, password);
        }

        public string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(password);
        }
    }
}