using System;
using System.Threading.Tasks;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// JWT token model for Auth feature
    /// </summary>
    public class JwtToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// User with authentication details
    /// </summary>
    public class UserWithAuth
    {
        public User User { get; set; } = null!;
        public string PasswordHash { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string? PronouncedName { get; set; }
        public string? Pronouns { get; set; }
        public DateTime? LastLoginAt { get; set; }
        
        // Convenience properties
        public Guid Id => User.Id;
        public EmailAddress Email => User.Email;
        public SceneName SceneName => User.SceneName;
        public UserRole Role => User.Role;
        public bool IsActive => User.IsActive;
    }

    /// <summary>
    /// Interface for JWT token operations in the Auth feature
    /// </summary>
    public interface IJwtService
    {
        JwtToken GenerateToken(UserWithAuth user);
        string GenerateRefreshToken();
    }
}