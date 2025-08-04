using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using WitchCityRope.Api.Interfaces;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Security;

namespace WitchCityRope.Api.Services
{
    /// <summary>
    /// Adapter to bridge the infrastructure JwtTokenService with the API IJwtService interface
    /// </summary>
    public class JwtServiceAdapter : IJwtService
    {
        private readonly JwtTokenService _jwtTokenService;

        public JwtServiceAdapter(JwtTokenService jwtTokenService)
        {
            _jwtTokenService = jwtTokenService;
        }

        public string GenerateToken(WitchCityRopeUser user)
        {
            return _jwtTokenService.GenerateToken(user);
        }

        public string GenerateToken(Guid userId, string email, string role, Dictionary<string, string>? additionalClaims = null)
        {
            // For now, throw NotImplementedException since JwtTokenService only accepts User objects
            // TODO: Update JwtTokenService to support generating tokens from claims directly
            throw new NotImplementedException("Generating tokens from individual parameters is not yet implemented");
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            return _jwtTokenService.ValidateToken(token);
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