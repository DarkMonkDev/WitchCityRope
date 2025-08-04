using System;
using System.Threading.Tasks;
using WitchCityRope.Core.Models.Auth;

namespace WitchCityRope.Core.Interfaces
{
    /// <summary>
    /// Service interface for identity-related operations
    /// </summary>
    public interface IIdentityService
    {
        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request">Registration request containing user details</param>
        /// <returns>Registration response with user ID and verification status</returns>
        Task<RegisterResponse> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Refreshes an access token using a refresh token
        /// </summary>
        /// <param name="refreshToken">The refresh token</param>
        /// <returns>New login response with fresh tokens</returns>
        Task<LoginResponse> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Verifies a user's email address
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="token">The verification token</param>
        /// <returns>True if verification succeeded, false otherwise</returns>
        Task<bool> VerifyEmailAsync(Guid userId, string token);
    }
}