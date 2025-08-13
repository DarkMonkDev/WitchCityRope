using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Infrastructure.Security
{
    /// <summary>
    /// Service for generating and validating JWT tokens
    /// </summary>
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _secretKey;
        private readonly int _expirationMinutes;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _issuer = configuration["Jwt:Issuer"] ?? configuration["JwtSettings:Issuer"] ?? "WitchCityRope";
            _audience = configuration["Jwt:Audience"] ?? configuration["JwtSettings:Audience"] ?? "WitchCityRopeUsers";
            _secretKey = configuration["Jwt:Secret"] ?? configuration["JwtSettings:SecretKey"];
            _expirationMinutes = configuration.GetValue<int>("Jwt:ExpiresInMinutes", configuration.GetValue<int>("JwtSettings:ExpirationMinutes", 60));

            if (string.IsNullOrEmpty(_secretKey))
            {
                throw new InvalidOperationException("JWT secret key not configured");
            }
        }

        /// <summary>
        /// Generates a JWT token for a user
        /// </summary>
        public string GenerateToken(WitchCityRopeUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("unique_name", user.SceneNameValue ?? string.Empty), // Use JWT standard claim name
                new Claim("role", user.Role.ToString()), // Use simple role claim name for JWT
                new Claim("UserId", user.Id.ToString()),
                new Claim("SceneName", user.SceneNameValue ?? string.Empty)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


        /// <summary>
        /// Validates a JWT token and returns the claims principal
        /// </summary>
        public ClaimsPrincipal ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return principal;
            }
            catch (Exception)
            {
                // Token validation failed
                return null;
            }
        }

        /// <summary>
        /// Gets user ID from a JWT token
        /// </summary>
        public Guid? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null)
                return null;

            var userIdClaim = principal.FindFirst("UserId") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// Generates a refresh token
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }
    }
}