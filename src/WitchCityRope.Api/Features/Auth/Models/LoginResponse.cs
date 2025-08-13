using System;
using WitchCityRope.Core.DTOs;

namespace WitchCityRope.Api.Features.Auth.Models
{
    /// <summary>
    /// Response model for successful login
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// JWT access token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token for obtaining new access tokens
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// When the access token expires
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// User information
        /// </summary>
        public UserDto User { get; set; } = new();

        /// <summary>
        /// User ID for compatibility with tests
        /// </summary>
        public Guid UserId { get; set; }
    }
}