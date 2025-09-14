using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services;

public interface IUserService
{
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<bool> EnableTwoFactorAsync(Guid userId, string code);
    Task<bool> DisableTwoFactorAsync(Guid userId, string password);
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Pronouns { get; set; }
    public bool PublicProfile { get; set; }
    public bool EmailVerified { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}