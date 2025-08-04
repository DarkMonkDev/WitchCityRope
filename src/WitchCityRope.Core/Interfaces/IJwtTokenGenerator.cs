using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WitchCityRope.Core.Interfaces
{
    /// <summary>
    /// Service interface for JWT token generation
    /// </summary>
    public interface IJwtTokenGenerator
    {
        /// <summary>
        /// Generates JWT tokens for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="email">The user's email</param>
        /// <param name="sceneName">The user's scene name</param>
        /// <param name="roles">The user's roles</param>
        /// <param name="claims">Additional claims for the user</param>
        /// <returns>Tuple containing access token, refresh token, and expiration date</returns>
        Task<(string Token, string RefreshToken, DateTime ExpiresAt)> GenerateTokenAsync(
            Guid userId,
            string email,
            string sceneName,
            IList<string> roles, 
            IList<Claim> claims);
    }
}