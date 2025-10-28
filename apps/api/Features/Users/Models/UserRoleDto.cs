using WitchCityRope.Api.Features.Users.Constants;

namespace WitchCityRope.Api.Features.Users.Models;

/// <summary>
/// DTO for exposing user role information.
/// This ensures the UserRole enum is included in OpenAPI specification
/// and auto-generated to TypeScript for frontend type safety.
/// </summary>
public class UserRoleDto
{
    /// <summary>
    /// The user role. This enum is auto-generated to TypeScript.
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Display name for the role (e.g., "Safety Team" instead of "SafetyTeam")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this role can do
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Response containing all available user roles in the system.
/// Used for dropdowns and role selection UI.
/// </summary>
public class AvailableRolesResponse
{
    /// <summary>
    /// List of all available roles with their display information
    /// </summary>
    public List<UserRoleDto> Roles { get; set; } = new();
}
