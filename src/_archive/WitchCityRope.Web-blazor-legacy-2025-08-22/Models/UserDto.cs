using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Web.Models;

// Moving models to separate file to avoid circular references
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public bool IsVetted { get; set; }
    public bool IsAdmin { get; set; }
    public bool EmailVerified { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public List<string> Roles { get; set; } = new();
    public UserRole Role { get; set; }
    public VettingStatus VettingStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UserProfileDto : UserDto
{
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
}