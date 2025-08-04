using System;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Api.Features.Auth.Models
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
        public WitchCityRopeUser User { get; set; } = null!;
        public string PasswordHash { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string? PronouncedName { get; set; }
        public string? Pronouns { get; set; }
        public DateTime? LastLoginAt { get; set; }
        
        // Convenience properties
        public Guid Id => User.Id;
        public EmailAddress Email => User.EmailAddress;
        public SceneName SceneName => User.SceneName;
        public UserRole Role => User.Role;
        public bool IsActive => User.IsActive;
    }
}