using System;

namespace WitchCityRope.Core.Entities
{
    public class UserAuthentication : BaseEntity
    {
        public Guid UserId { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string? TwoFactorSecret { get; set; }
        public bool IsTwoFactorEnabled { get; set; }
        public DateTime? LastPasswordChangeAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockedOutUntil { get; set; }
        public string? EmailVerificationToken { get; set; }
        public DateTime EmailVerificationTokenCreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public User User { get; set; } = null!;
    }
}