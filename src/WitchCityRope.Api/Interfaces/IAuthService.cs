using System.Threading.Tasks;
using WitchCityRope.Api.Features.Auth.Models;

namespace WitchCityRope.Api.Interfaces
{
    /// <summary>
    /// Interface for authentication service operations
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates a user with email and password
        /// </summary>
        Task<LoginResponse> LoginAsync(LoginRequest request);

        /// <summary>
        /// Registers a new user
        /// </summary>
        Task<RegisterResponse> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Refreshes an expired JWT token
        /// </summary>
        Task<LoginResponse> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Verifies a user's email address with the provided token
        /// </summary>
        Task VerifyEmailAsync(string token);

        /// <summary>
        /// Authenticates a web service user and returns JWT token
        /// </summary>
        Task<LoginResponse> WebServiceLoginAsync(WebServiceLoginRequest request);

        /// <summary>
        /// Gets a JWT token for a user on behalf of the Web service
        /// </summary>
        Task<LoginResponse> GetServiceTokenAsync(string userId, string email);
    }
}