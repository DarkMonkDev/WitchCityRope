using System;
using System.Security.Cryptography;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Infrastructure.Security;

namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// Adapter to make JwtTokenService work with Auth.Services.IJwtService interface
    /// </summary>
    public class JwtServiceAdapter : IJwtService
    {
        private readonly JwtTokenService _jwtTokenService;

        public JwtServiceAdapter(JwtTokenService jwtTokenService)
        {
            _jwtTokenService = jwtTokenService;
        }

        public JwtToken GenerateToken(UserWithAuth user)
        {
            var token = _jwtTokenService.GenerateToken(user.User);
            
            // JwtTokenService returns the token string, but we need a JwtToken object
            return new JwtToken
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1) // Default 1 hour expiry
            };
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
    }
}