using System;

namespace WitchCityRope.Infrastructure.Data.Entities
{
    /// <summary>
    /// Entity to store user authentication data separate from core user data
    /// </summary>
    public class UserAuthentication
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string? EmailVerificationToken { get; set; }
        public DateTime EmailVerificationTokenCreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        
        // Navigation property
        public virtual WitchCityRope.Core.Entities.User User { get; set; } = null!;
    }
}