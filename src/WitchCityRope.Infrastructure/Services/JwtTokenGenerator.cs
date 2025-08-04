using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.Entities;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Infrastructure.Services
{
    /// <summary>
    /// Service implementation for generating JWT tokens for API authentication
    /// </summary>
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;
        private readonly WitchCityRopeIdentityDbContext _context;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _secretKey;
        private readonly int _accessTokenExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        public JwtTokenGenerator(
            IConfiguration configuration,
            WitchCityRopeIdentityDbContext context)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            
            // Load JWT settings
            var jwtSection = _configuration.GetSection("JwtSettings");
            _issuer = jwtSection["Issuer"] ?? "WitchCityRope";
            _audience = jwtSection["Audience"] ?? "WitchCityRope";
            _secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured");
            _accessTokenExpirationMinutes = jwtSection.GetValue<int>("AccessTokenExpirationMinutes", 60); // Default 60 minutes
            _refreshTokenExpirationDays = jwtSection.GetValue<int>("RefreshTokenExpirationDays", 30); // Default 30 days

            // Validate secret key length
            if (_secretKey.Length < 32)
            {
                throw new InvalidOperationException("JWT secret key must be at least 32 characters long");
            }
        }

        public async Task<(string Token, string RefreshToken, DateTime ExpiresAt)> GenerateTokenAsync(
            Guid userId,
            string email,
            string sceneName,
            IList<string> roles,
            IList<Claim> claims)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Invalid user ID", nameof(userId));
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required", nameof(email));
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new ArgumentException("Scene name is required", nameof(sceneName));

            // Generate access token
            var accessToken = GenerateAccessToken(userId, email, sceneName, roles, claims);
            var expiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);

            // Generate refresh token
            var refreshToken = GenerateRefreshToken();

            // Store refresh token in database
            var refreshTokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };

            // Find the user and add the refresh token
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();
            }

            return (accessToken, refreshToken, expiresAt);
        }

        private string GenerateAccessToken(
            Guid userId,
            string email,
            string sceneName,
            IList<string> roles,
            IList<Claim> existingClaims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            // Build claims list
            var claims = new List<Claim>
            {
                // Standard claims
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, sceneName),
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, 
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), 
                    ClaimValueTypes.Integer64),
                
                // Custom claims
                new Claim("UserId", userId.ToString()),
                new Claim("SceneName", sceneName)
            };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add any additional claims passed in
            if (existingClaims != null && existingClaims.Any())
            {
                // Filter out duplicate claims
                var existingClaimTypes = claims.Select(c => c.Type).ToHashSet();
                foreach (var claim in existingClaims)
                {
                    if (!existingClaimTypes.Contains(claim.Type))
                    {
                        claims.Add(claim);
                    }
                }
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                NotBefore = DateTime.UtcNow
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            // Generate a cryptographically secure random refresh token
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", ""); // URL-safe base64
            }
        }
    }
}