using WitchCityRope.Api.Features.Auth.Models;

namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// Interface for JWT token operations in the Auth feature
    /// </summary>
    public interface IJwtService
    {
        JwtToken GenerateToken(UserWithAuth user);
        string GenerateRefreshToken();
    }
}