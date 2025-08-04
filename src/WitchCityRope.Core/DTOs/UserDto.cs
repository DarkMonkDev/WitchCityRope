using System;
using System.Collections.Generic;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Core.DTOs
{
    /// <summary>
    /// User information DTO - consolidated from multiple projects
    /// </summary>
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SceneName { get; set; } = string.Empty;
        public string PronouncedName { get; set; } = string.Empty;
        public string Pronouns { get; set; } = string.Empty;
        public bool IsVetted { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }
        public bool EmailVerified { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new();
        public UserRole Role { get; set; }
        public VettingStatus VettingStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>
    /// Extended user profile DTO with additional details
    /// </summary>
    public class UserProfileDto : UserDto
    {
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
    }

    /// <summary>
    /// Detailed user DTO with additional information
    /// </summary>
    public class UserDetailsDto : UserDto
    {
        public int Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int RegistrationCount { get; set; }
        public int VettingApplicationCount { get; set; }
    }
}