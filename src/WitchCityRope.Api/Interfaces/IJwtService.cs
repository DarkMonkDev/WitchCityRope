using System;
using System.Collections.Generic;
using System.Security.Claims;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Api.Interfaces
{
    /// <summary>
    /// Interface for JWT token generation and validation
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generates a JWT token for the specified user
        /// </summary>
        string GenerateToken(WitchCityRopeUser user);

        /// <summary>
        /// Generates a JWT token with custom claims
        /// </summary>
        string GenerateToken(Guid userId, string email, string role, Dictionary<string, string>? additionalClaims = null);

        /// <summary>
        /// Validates a JWT token and returns the claims principal
        /// </summary>
        ClaimsPrincipal? ValidateToken(string token);

        /// <summary>
        /// Generates a refresh token
        /// </summary>
        string GenerateRefreshToken();
    }
}