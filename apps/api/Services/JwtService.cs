using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Models.Auth;

namespace WitchCityRope.Api.Services;

/// <summary>
/// JWT token generation and validation service implementation
/// For authentication vertical slice test - throwaway implementation
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;
    private readonly ITokenBlacklistService _tokenBlacklistService;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger, ITokenBlacklistService tokenBlacklistService)
    {
        _configuration = configuration;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
        _tokenBlacklistService = tokenBlacklistService;

        // Get configuration values with defaults for development
        _secretKey = _configuration["Jwt:SecretKey"] ?? "DevSecret-JWT-WitchCityRope-AuthTest-2024-32CharMinimum!";
        _issuer = _configuration["Jwt:Issuer"] ?? "WitchCityRope-API";
        _audience = _configuration["Jwt:Audience"] ?? "WitchCityRope-Services";
        _expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

        // Validate secret key length for security
        if (_secretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT secret key must be at least 32 characters long");
        }
    }

    /// <summary>
    /// Generate JWT token for authenticated user with proper claims structure
    /// </summary>
    public JwtToken GenerateToken(ApplicationUser user)
    {
        try
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("scene_name", user.SceneName ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? "Member"), // Add role claim for authorization
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = credentials
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = _tokenHandler.WriteToken(token);

            _logger.LogDebug("Generated JWT token for user {UserId} with expiration {Expiration}",
                user.Id, tokenDescriptor.Expires);

            return new JwtToken
            {
                Token = tokenString,
                ExpiresAt = tokenDescriptor.Expires.Value
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JWT token for user {UserId}", user.Id);
            throw;
        }
    }

    /// <summary>
    /// Validate JWT token structure and signature
    /// Also checks if the token is blacklisted (logged out)
    /// </summary>
    public bool ValidateToken(string token)
    {
        try
        {
            // First check if token is blacklisted
            var jti = ExtractJti(token);
            if (!string.IsNullOrEmpty(jti) && _tokenBlacklistService.IsTokenBlacklisted(jti))
            {
                _logger.LogDebug("Token validation failed: token is blacklisted (user logged out)");
                return false;
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minute clock skew
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return validatedToken != null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Token validation failed: {Error}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Check if token is near expiry (within 30 minutes) for refresh purposes
    /// </summary>
    public bool IsTokenNearExpiry(string token)
    {
        try
        {
            var jsonToken = _tokenHandler.ReadJwtToken(token);
            var expiry = jsonToken.ValidTo;
            var now = DateTime.UtcNow;

            // Token is near expiry if it expires within the next 30 minutes
            return expiry.Subtract(now).TotalMinutes <= 30;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Failed to check token expiry: {Error}", ex.Message);
            return true; // Assume near expiry if we can't parse
        }
    }

    /// <summary>
    /// Validate token structure without checking expiry (for refresh scenarios)
    /// </summary>
    public bool ValidateTokenStructure(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // Skip lifetime validation for refresh
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return validatedToken != null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Token structure validation failed: {Error}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Extract the JTI (JWT ID) from a token for blacklisting purposes
    /// </summary>
    public string? ExtractJti(string token)
    {
        try
        {
            var jsonToken = _tokenHandler.ReadJwtToken(token);
            return jsonToken?.Claims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Failed to extract JTI from token: {Error}", ex.Message);
            return null;
        }
    }
}